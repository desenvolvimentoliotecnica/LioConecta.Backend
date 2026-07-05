using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task AddEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);

    Task<int> CountEventsByTypeAsync(
        string eventType,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<AnalyticsSnapshotDto> GetSnapshotAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string periodKey,
        CancellationToken cancellationToken = default);
}
