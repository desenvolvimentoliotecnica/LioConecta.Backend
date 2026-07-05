using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Configuration;
using LioConecta.Infrastructure.Integrations.Glpi;
using LioConecta.Infrastructure.Integrations.Graph;
using LioConecta.Infrastructure.Integrations.Totvs;
using LioConecta.Infrastructure.Persistence;
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

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

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
    }

    private static void RegisterIntegrations(IServiceCollection services, IAppSettingsProvider settings)
    {
        services.AddSingleton<IOptions<IntegrationsOptions>>(_ => Options.Create(new IntegrationsOptions
        {
            UseDevAdapters = settings.GetBool(AppSettingKeys.IntegrationsUseDevAdapters, true),
        }));

        services.AddSingleton<IOptions<TotvsOptions>>(_ => Options.Create(new TotvsOptions
        {
            BaseUrl = settings.GetString(AppSettingKeys.TotvsBaseUrl),
            ApiKey = settings.GetString(AppSettingKeys.TotvsApiKey),
        }));

        services.AddSingleton<IOptions<GlpiOptions>>(_ => Options.Create(new GlpiOptions
        {
            BaseUrl = settings.GetString(AppSettingKeys.GlpiBaseUrl),
            AppToken = settings.GetString(AppSettingKeys.GlpiAppToken),
            UserToken = settings.GetString(AppSettingKeys.GlpiUserToken),
        }));

        services.AddSingleton<IOptions<GraphOptions>>(_ => Options.Create(new GraphOptions
        {
            TenantId = settings.GetString(AppSettingKeys.GraphTenantId),
            ClientId = settings.GetString(AppSettingKeys.GraphClientId),
            ClientSecret = settings.GetString(AppSettingKeys.GraphClientSecret),
        }));

        if (settings.GetBool(AppSettingKeys.IntegrationsUseDevAdapters, true))
        {
            services.AddSingleton<ITotvsAdapter, DevTotvsAdapter>();
            services.AddSingleton<IGlpiAdapter, DevGlpiAdapter>();
            services.AddSingleton<IGraphAdapter, DevGraphAdapter>();
            return;
        }

        services.AddHttpClient<ITotvsAdapter, TotvsAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TotvsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddHttpClient<IGlpiAdapter, GlpiAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GlpiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddHttpClient<IGraphAdapter, GraphAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GraphOptions>>().Value;
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            _ = options;
        });
    }

    private static void RegisterServices(IServiceCollection services, IAppSettingsProvider settings)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IComunicadoHeroImageService, ComunicadoHeroImageService>();
        services.AddScoped<SeedDataService>();

        var redisConnection = settings.GetRedisConnection();
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));
        }
    }

    public sealed class IntegrationsOptions
    {
        public const string SectionName = "Integrations";

        public bool UseDevAdapters { get; set; } = true;
    }
}
