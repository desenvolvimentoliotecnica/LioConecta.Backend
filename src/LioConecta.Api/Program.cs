using LioConecta.Api.Auth;
using LioConecta.Api.Authorization;
using LioConecta.Api.Hubs;
using LioConecta.Api.Middleware;
using LioConecta.Application;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var configuration = builder.Configuration;
    var useDevAuth = configuration.GetValue("Auth:UseDevAuth", builder.Environment.IsDevelopment());
    var azureClientId = configuration["AzureAd:ClientId"];
    var useDevAuthentication = string.IsNullOrWhiteSpace(azureClientId);

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(configuration);

    var authenticationBuilder = builder.Services.AddAuthentication(options =>
    {
        if (useDevAuthentication)
        {
            options.DefaultAuthenticateScheme = DevAuthDefaults.SchemeName;
            options.DefaultChallengeScheme = DevAuthDefaults.SchemeName;
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
        authenticationBuilder.AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
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

        if (useDevAuth && builder.Environment.IsDevelopment())
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

    var allowedOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:5173"];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var postgresConnection = configuration.GetConnectionString("DefaultConnection");
    var redisConnection = configuration.GetConnectionString("Redis");

    var healthChecks = builder.Services.AddHealthChecks();

    if (!string.IsNullOrWhiteSpace(postgresConnection))
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
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHub<ChatHub>("/hubs/chat");

    if (app.Environment.IsEnvironment("Testing"))
    {
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
    }
    else
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        });
    }

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var seedDataService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedDataService.EnsureSeededAsync();
    }

    Log.Information("LioConecta API started. DevAuth={DevAuth}, DevAuthentication={DevAuthentication}",
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
