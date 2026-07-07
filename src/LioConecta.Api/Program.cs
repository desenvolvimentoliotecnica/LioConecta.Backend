using LioConecta.Api.Auth;
using LioConecta.Api.Authorization;
using LioConecta.Api.Extensions;
using LioConecta.Api.Hubs;
using LioConecta.Api.Middleware;
using LioConecta.Api.Services;
using LioConecta.Application;
using LioConecta.Application.Common;
using LioConecta.Application.Common.Audit;
using LioConecta.Application.Common.Observability;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure;
using LioConecta.Infrastructure.Configuration;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using LioConecta.Workers.Jobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var settingsProvider = await CreateSettingsProviderAsync(builder.Environment);

    var logLevel = ParseLogLevel(settingsProvider.GetString(AppSettingKeys.SerilogDefaultLevel, "Information"));
    builder.Host.UseSerilog((_, services, configuration) => configuration
        .MinimumLevel.Is(logLevel)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var useDevAuth = settingsProvider.GetBool(AppSettingKeys.AuthUseDevAuth, builder.Environment.IsDevelopment());
    var authProvider = settingsProvider.GetString(AppSettingKeys.AuthProvider, "ldap").Trim().ToLowerInvariant();
    var useDevAuthentication = builder.Environment.IsEnvironment("Testing")
        || (authProvider == "dev" && builder.Environment.IsDevelopment());
    var jwtSigningKey = settingsProvider.GetString(AppSettingKeys.AuthJwtSigningKey);

    builder.Services.AddSingleton<IAppSettingsProvider>(settingsProvider);
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(settingsProvider);
    builder.Services.AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>();
    builder.Services.AddScoped<IChatBroadcaster, SignalRChatBroadcaster>();
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddHostedService<TotvsSyncWorker>();
        builder.Services.AddHostedService<GraphSyncWorker>();
        builder.Services.AddHostedService<GraphDirectorySyncWorker>();
        builder.Services.AddHostedService<PollClosureWorker>();
        builder.Services.AddHostedService<TotvsTimesheetSyncWorker>();
        builder.Services.AddHostedService<TotvsPayslipSyncWorker>();
        builder.Services.AddHostedService<EmailDispatchWorker>();
    }

    builder.Services.AddHostedService<ObservabilityRetentionHostedService>();

    var authenticationBuilder = builder.Services.AddAuthentication(options =>
    {
        if (useDevAuthentication)
        {
            options.DefaultAuthenticateScheme = DevAuthDefaults.SchemeName;
            options.DefaultChallengeScheme = DevAuthDefaults.SchemeName;
        }
        else
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }
    });

    if (useDevAuthentication)
    {
        authenticationBuilder.AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
            DevAuthDefaults.SchemeName,
            _ => { });
    }
    else
    {
        if (string.IsNullOrWhiteSpace(jwtSigningKey))
        {
            throw new InvalidOperationException("auth.jwt_signing_key é obrigatório quando auth.provider=ldap.");
        }

        authenticationBuilder.AddJwtBearer(options =>
        {
            options.TokenValidationParameters = PortalJwtService.BuildValidationParameters(jwtSigningKey);

            if (settingsProvider.GetBool(AppSettingKeys.ObservabilityAuthAuditEnabled, true))
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = async context =>
                    {
                        var recorder = context.HttpContext.RequestServices
                            .GetRequiredService<IAccessAuditRecorder>();
                        var correlationId = ResolveAuthCorrelationId(context.HttpContext);
                        await recorder.RecordAsync(new AccessAuditEntry(
                            EventType: AccessEventTypes.Authentication,
                            EventName: ObservabilityEventNames.Authentication.LoginFailed,
                            CorrelationId: correlationId,
                            UserId: null,
                            UsernameSnapshot: null,
                            SessionId: null,
                            Resource: context.Request.Path.Value,
                            Action: "jwt_validate",
                            Result: AccessEventResults.Failed,
                            ReasonCode: context.Exception.GetType().Name,
                            StatusCode: StatusCodes.Status401Unauthorized,
                            HttpMethod: context.Request.Method,
                            Path: context.Request.Path.Value));
                    },
                    OnTokenValidated = async context =>
                    {
                        var recorder = context.HttpContext.RequestServices
                            .GetRequiredService<IAccessAuditRecorder>();
                        var correlationId = ResolveAuthCorrelationId(context.HttpContext);
                        var username = context.Principal?.FindFirst("preferred_username")?.Value;
                        await recorder.RecordAsync(new AccessAuditEntry(
                            EventType: AccessEventTypes.Authentication,
                            EventName: ObservabilityEventNames.Authentication.LoginSucceeded,
                            CorrelationId: correlationId,
                            UserId: null,
                            UsernameSnapshot: username,
                            SessionId: null,
                            Resource: context.Request.Path.Value,
                            Action: "jwt_validate",
                            Result: AccessEventResults.Success,
                            ReasonCode: null,
                            StatusCode: StatusCodes.Status200OK,
                            HttpMethod: context.Request.Method,
                            Path: context.Request.Path.Value));
                    },
                };
            }
        });
    }

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthPolicies.RequireHR, policy =>
            policy.RequireRole(UserRole.HR.ToString()));

        options.AddPolicy(AuthPolicies.RequireAdmin, policy =>
            policy.RequireRole(
                UserRole.Admin.ToString(),
                UserRole.AnalyticsViewer.ToString()));

        options.AddPolicy(AuthPolicies.RequireTI, policy =>
            policy.RequireRole(UserRole.TI.ToString()));

        options.AddPolicy(AuthPolicies.RequireKioskReader, policy =>
            policy.RequireRole(UserRole.KioskReader.ToString()));

        if ((useDevAuth && builder.Environment.IsDevelopment()) || builder.Environment.IsEnvironment("Testing"))
        {
            options.FallbackPolicy = null;
        }
        else
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        }
    });

    var allowedOrigins = settingsProvider.GetStringArray(AppSettingKeys.CorsAllowedOrigins);
    if (allowedOrigins.Count == 0)
    {
        allowedOrigins = ["http://localhost:5173", "http://localhost:5174"];
    }

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.SetIsOriginAllowed(origin =>
                        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                        uri.Host is "localhost" or "127.0.0.1")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else
            {
                policy.WithOrigins(allowedOrigins.ToArray())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ObservabilityAuthorizationResultHandler>();
    builder.Services.AddLioConectaObservability(settingsProvider, builder.Environment);

    var postgresConnection = settingsProvider.GetConnectionString();
    var redisConnection = settingsProvider.GetRedisConnection();

    var healthChecks = builder.Services.AddHealthChecks();

    if (!string.IsNullOrWhiteSpace(postgresConnection) &&
        !builder.Environment.IsEnvironment("Testing"))
    {
        healthChecks.AddNpgSql(postgresConnection, name: "postgres", tags: ["ready"]);
    }

    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        healthChecks.AddRedis(redisConnection, name: "redis", tags: ["ready"]);
    }

    var signalRBuilder = builder.Services.AddSignalR();
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
        {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("LioConecta");
        });
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseCors();

    app.UseMiddleware<CorrelationMiddleware>();

    var mediaRoot = ResolveComunicadoMediaRoot(settingsProvider, app.Environment);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mediaRoot),
        RequestPath = "/media/comunicados",
    });

    var postsMediaRoot = ResolvePostsMediaRoot(settingsProvider, app.Environment);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(postsMediaRoot),
        RequestPath = "/posts/medias",
    });

    var peopleMediaRoot = ResolvePeopleMediaRoot(settingsProvider, app.Environment);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(peopleMediaRoot),
        RequestPath = "/media/people",
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<AccessAuditMiddleware>();
    app.UseMiddleware<AuditMiddleware>();
    app.UseMiddleware<TransactionAuditMiddleware>();
    app.UseMiddleware<ObservabilityLoggingMiddleware>();
    app.UseMiddleware<AuditLoggingMiddleware>();
    app.UseMiddleware<AuditTrailMiddleware>();

    app.UseLioConectaObservability(settingsProvider);

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHub<ChatHub>("/hubs/chat");

    if (app.Environment.IsEnvironment("Testing"))
    {
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
    }
    else
    {
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        }).AllowAnonymous();
    }

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var seedDataService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedDataService.EnsureSeededAsync();
    }

    Log.Information("LioConecta API started. AuthProvider={AuthProvider}, DevAuth={DevAuth}, DevAuthentication={DevAuthentication}",
        authProvider,
        useDevAuth,
        useDevAuthentication);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static async Task<IAppSettingsProvider> CreateSettingsProviderAsync(IWebHostEnvironment environment)
{
    var provider = new AppSettingsProvider();

    if (environment.IsEnvironment("Testing"))
    {
        provider.Reload(BuildTestingDefaults());
        return provider;
    }

    var devFallback = environment.IsDevelopment()
        ? "Host=localhost;Port=5433;Database=lioconecta;Username=lioconecta;Password=lioconecta_dev"
        : null;

    var bootstrapConnection = BootstrapConnection.Resolve(devFallback);
    var values = await AppSettingsSeeder.LoadValuesAsync(bootstrapConnection);
    provider.Reload(values);
    return provider;
}

static Dictionary<string, string> BuildTestingDefaults()
{
    var defaults = AppSettingCatalog.All.ToDictionary(
        d => d.Key,
        d => d.DefaultValue,
        StringComparer.OrdinalIgnoreCase);

    defaults[AppSettingKeys.AuthProvider] = "dev";
    return defaults;
}

static Microsoft.Extensions.Configuration.IConfiguration BuildAzureAdConfiguration(IAppSettingsProvider settings)
{
    return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureAd:Instance"] = settings.GetString(AppSettingKeys.AzureAdInstance),
            ["AzureAd:TenantId"] = settings.GetString(AppSettingKeys.AzureAdTenantId),
            ["AzureAd:ClientId"] = settings.GetString(AppSettingKeys.AzureAdClientId),
            ["AzureAd:Audience"] = settings.GetString(AppSettingKeys.AzureAdAudience),
        })
        .Build()
        .GetSection("AzureAd");
}

static LogEventLevel ParseLogLevel(string value) =>
    Enum.TryParse<LogEventLevel>(value, true, out var level) ? level : LogEventLevel.Information;

static string ResolveComunicadoMediaRoot(IAppSettingsProvider settings, IWebHostEnvironment environment)
{
    var configured = settings.GetString(AppSettingKeys.MediaComunicadosRootPath, "App_Data/media/comunicados");
    var absolute = Path.IsPathRooted(configured)
        ? configured
        : Path.Combine(environment.ContentRootPath, configured);

    Directory.CreateDirectory(absolute);
    return absolute;
}

static string ResolvePostsMediaRoot(IAppSettingsProvider settings, IWebHostEnvironment environment)
{
    var configured = settings.GetString(AppSettingKeys.MediaPostsRootPath, "App_Data/posts/medias");
    var absolute = Path.IsPathRooted(configured)
        ? configured
        : Path.Combine(environment.ContentRootPath, configured);

    Directory.CreateDirectory(absolute);
    return absolute;
}

static string ResolvePeopleMediaRoot(IAppSettingsProvider settings, IWebHostEnvironment environment)
{
    var configured = settings.GetString(AppSettingKeys.MediaPeopleRootPath, "App_Data/media/people");
    var absolute = Path.IsPathRooted(configured)
        ? configured
        : Path.Combine(environment.ContentRootPath, configured);

    Directory.CreateDirectory(absolute);
    return absolute;
}

static Guid ResolveAuthCorrelationId(HttpContext httpContext)
{
    if (httpContext.Items[AuditContext.HttpContextItemKey] is AuditContext auditContext)
    {
        return auditContext.CorrelationId;
    }

    var header = httpContext.Request.Headers[AuditContext.CorrelationHeaderName].FirstOrDefault();
    return Guid.TryParse(header, out var parsed) ? parsed : Guid.NewGuid();
}
