using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Glpi;
using LioConecta.Infrastructure.Integrations.Graph;
using LioConecta.Infrastructure.Integrations.Totvs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Persistence.Repositories;
using LioConecta.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.Infrastructure;

public static class DependencyInjection
{
    public sealed class IntegrationsOptions
    {
        public const string SectionName = "Integrations";

        public bool UseDevAdapters { get; set; } = true;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHttpContextAccessor();

        RegisterRepositories(services);
        RegisterIntegrations(services, configuration);
        RegisterServices(services, configuration);

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
    }

    private static void RegisterIntegrations(IServiceCollection services, IConfiguration configuration)
    {
        var integrations = configuration
            .GetSection(IntegrationsOptions.SectionName)
            .Get<IntegrationsOptions>() ?? new IntegrationsOptions();

        services.Configure<IntegrationsOptions>(configuration.GetSection(IntegrationsOptions.SectionName));
        services.Configure<TotvsOptions>(configuration.GetSection(TotvsOptions.SectionName));
        services.Configure<GlpiOptions>(configuration.GetSection(GlpiOptions.SectionName));
        services.Configure<GraphOptions>(configuration.GetSection(GraphOptions.SectionName));

        if (integrations.UseDevAdapters)
        {
            services.AddSingleton<ITotvsAdapter, DevTotvsAdapter>();
            services.AddSingleton<IGlpiAdapter, DevGlpiAdapter>();
            services.AddSingleton<IGraphAdapter, DevGraphAdapter>();
            return;
        }

        services.AddHttpClient<ITotvsAdapter, TotvsAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TotvsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddHttpClient<IGlpiAdapter, GlpiAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GlpiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddHttpClient<IGraphAdapter, GraphAdapter>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GraphOptions>>().Value;
            client.BaseAddress = new Uri($"https://graph.microsoft.com/v1.0/");
            _ = options;
        });
    }

    private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<SeedDataService>();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));
        }
    }
}
