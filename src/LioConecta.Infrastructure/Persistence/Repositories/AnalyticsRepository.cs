using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class AnalyticsRepository(AppDbContext db) : IAnalyticsRepository
{
    public Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        db.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<AnalyticsEvent>)t.Result, cancellationToken);

    public async Task AddEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        db.AnalyticsEvents.Add(analyticsEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountEventsByTypeAsync(
        string eventType,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        db.AnalyticsEvents.CountAsync(
            e => e.EventType == eventType && e.OccurredAt >= from && e.OccurredAt <= to,
            cancellationToken);
}
