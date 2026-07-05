using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class AnalyticsService(
    IAnalyticsRepository analyticsRepository,
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService) : IAnalyticsService
{
    public async Task<AnalyticsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var from = DateTimeOffset.UtcNow.AddDays(-30);
        var to = DateTimeOffset.UtcNow;
        var events = await analyticsRepository.GetEventsAsync(from, to, cancellationToken);
        var eventsByType = events
            .GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var unread = await notificationRepository.GetUnreadCountAsync(personId, cancellationToken);

        return new AnalyticsDashboardDto(
            ActiveUsers: events.Where(e => e.PersonId is not null).Select(e => e.PersonId).Distinct().Count(),
            FeedPosts: eventsByType.GetValueOrDefault("FeedPostCreated"),
            FeedReactions: eventsByType.GetValueOrDefault("FeedPostLiked"),
            ServiceRequests: eventsByType.GetValueOrDefault("ServiceRequestCreated"),
            UnreadNotifications: unread,
            MoodChecks: eventsByType.GetValueOrDefault("MoodCheckRecorded"),
            EventsByType: eventsByType);
    }
}
