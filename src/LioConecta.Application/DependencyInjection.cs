using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IComunicadoService, ComunicadoService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IActivityService, ActivityService>();

        return services;
    }
}
