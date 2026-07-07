using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Configuration;
using LioConecta.Infrastructure.Integrations.Email;
using LioConecta.Infrastructure.Integrations.Glpi;
using LioConecta.Infrastructure.Integrations.Graph;
using LioConecta.Infrastructure.Integrations.Ldap;
using LioConecta.Infrastructure.Integrations.Totvs;
using LioConecta.Application.Common.Audit;
using LioConecta.Infrastructure.Integrations.TotvsRm;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Persistence.Interceptors;
using LioConecta.Infrastructure.Persistence.Repositories;
using LioConecta.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LioConecta.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IAppSettingsProvider settings)
    {
        var connectionString = settings.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"App setting '{AppSettingKeys.DatabaseDefaultConnection}' is required.");
        }

        services.AddSingleton<IAuditContextAccessor, AuditContextAccessor>();
        services.AddSingleton<ChangeAuditInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<ChangeAuditInterceptor>());
        });

        services.AddHttpContextAccessor();

        RegisterRepositories(services);
        RegisterIntegrations(services, settings);
        RegisterServices(services, settings);

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IFeedRepository, FeedRepository>();
        services.AddScoped<IComunicadoRepository, ComunicadoRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ICalendarRepository, CalendarRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IMoodCheckRepository, MoodCheckRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IComunicadoHeroImageRepository, ComunicadoHeroImageRepository>();
        services.AddScoped<IPayslipRepository, PayslipRepository>();
        services.AddScoped<IBenefitRepository, BenefitRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IObservabilityRepository, ObservabilityRepository>();
        services.AddScoped<ITimesheetPeriodCacheRepository, TimesheetPeriodCacheRepository>();
    }

    private static void RegisterIntegrations(IServiceCollection services, IAppSettingsProvider settings)
    {
        services.AddSingleton<IOptions<TotvsOptions>>(_ => Options.Create(new TotvsOptions
        {
            BaseUrl = settings.GetString(AppSettingKeys.TotvsBaseUrl),
            ApiKey = settings.GetString(AppSettingKeys.TotvsApiKey),
        }));

        services.AddSingleton<IOptions<GraphOptions>>(_ => Options.Create(new GraphOptions
        {
            TenantId = settings.GetString(AppSettingKeys.GraphTenantId),
            ClientId = settings.GetString(AppSettingKeys.GraphClientId),
            ClientSecret = settings.GetString(AppSettingKeys.GraphClientSecret),
        }));

        services.AddSingleton<GlpiSessionManager>();
        services.AddSingleton<GlpiCredentialsResolver>();
        services.AddSingleton<GlpiUserNameResolver>();
        services.AddSingleton<LdapSettingsResolver>();
        services.AddSingleton<GraphTokenProvider>();
        services.AddTransient<GraphAuthDelegatingHandler>();

        services.AddHttpClient<IGlpiAdapter, GlpiAdapter>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<ITotvsAdapter, TotvsAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TotvsOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "http://127.0.0.1:9/"
                : options.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddHttpClient<IGraphAdapter, GraphAdapter>((sp, client) =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
        }).AddHttpMessageHandler<GraphAuthDelegatingHandler>();

        services.AddHttpClient<IPlannerAdapter, GraphPlannerAdapter>(client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.Timeout = TimeSpan.FromSeconds(90);
        }).AddHttpMessageHandler<GraphAuthDelegatingHandler>();

        services.AddScoped<ITotvsRmTimesheetRepository, TotvsRmTimesheetRepository>();
        services.AddScoped<ITotvsRmPayslipRepository, TotvsRmPayslipRepository>();
        services.AddScoped<ITotvsRmEmployeeRepository, TotvsRmEmployeeRepository>();
        services.AddScoped<TotvsRmConnectionTester>();
    }

    private static void RegisterServices(IServiceCollection services, IAppSettingsProvider settings)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAccessAuditRecorder, AccessAuditRecorder>();
        services.AddScoped<IComunicadoHeroImageService, ComunicadoHeroImageService>();
        services.AddScoped<IPostMediaService, PostMediaService>();
        services.AddScoped<SeedDataService>();
        services.AddScoped<ITotvsRmConfigurationService, TotvsRmConfigurationService>();
        services.AddScoped<IEmailConfigurationService, EmailConfigurationService>();
        services.AddScoped<IOrgChartGovernanceService, OrgChartGovernanceService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();
        services.AddScoped<IEmailDispatchService, EmailDispatchService>();
        services.AddScoped<IEmailAdminService, EmailAdminService>();
        services.AddScoped<IEmailAttachmentService, EmailAttachmentService>();
        services.AddScoped<IEmailSendService, EmailSendService>();
        services.AddScoped<ISmtpEmailSender, SmtpEmailSender>();
        services.AddScoped<ITotvsEmployeeSyncService, TotvsEmployeeSyncService>();
        services.AddScoped<IGraphSyncService, GraphSyncService>();
        services.AddScoped<IGraphDirectorySyncService, GraphDirectorySyncService>();
        services.AddScoped<IGraphConfigurationService, GraphConfigurationService>();
        services.AddScoped<IPlannerConfigurationService, PlannerConfigurationService>();
        services.AddScoped<IGlpiConfigurationService, GlpiConfigurationService>();
        services.AddScoped<ILdapConfigurationService, LdapConfigurationService>();
        services.AddScoped<ILdapAuthService, LdapAuthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPortalJwtService, PortalJwtService>();
        services.AddScoped<LdapConnectionTester>();
        services.AddScoped<IPersonPhotoStorageService, PersonPhotoStorageService>();
        services.AddScoped<GraphConnectionTester>();
        services.AddScoped<PlannerConnectionTester>();
        services.AddScoped<GlpiConnectionTester>();
        services.AddScoped<IWorkerRunRecorder, WorkerRunRecorder>();
        services.AddScoped<IWorkerTriggerService, WorkerTriggerService>();

        var redisConnection = settings.GetRedisConnection();
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));
        }
    }
}
