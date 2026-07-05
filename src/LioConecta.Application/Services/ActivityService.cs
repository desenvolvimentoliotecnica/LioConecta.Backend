using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class ActivityService(
    IAnalyticsRepository analyticsRepository,
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService) : IActivityService
{
    public async Task<IReadOnlyList<ActivityDto>> GetRecentAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var from = DateTimeOffset.UtcNow.AddDays(-14);
        var to = DateTimeOffset.UtcNow;

        var analyticsEvents = await analyticsRepository.GetEventsAsync(from, to, cancellationToken);
        var notificationPage = await notificationRepository.GetPageAsync(
            personId,
            new Common.CursorPageRequest { Limit = limit },
            cancellationToken);

        var activities = analyticsEvents
            .Where(e => e.PersonId == personId)
            .Select(e => new ActivityDto(
                e.Id,
                e.EventType,
                e.EventType,
                null,
                null,
                e.OccurredAt))
            .Concat(notificationPage.Items.Select(n => new ActivityDto(
                n.Id,
                n.Type.ToString(),
                n.Title,
                n.Body,
                n.Href,
                n.CreatedAt)))
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
            .ToList();

        return activities;
    }
}
