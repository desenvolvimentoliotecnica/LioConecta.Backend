using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task AddEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);

    Task<int> CountEventsByTypeAsync(string eventType, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
