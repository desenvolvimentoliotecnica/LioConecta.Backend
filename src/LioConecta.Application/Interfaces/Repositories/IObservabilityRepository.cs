using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IObservabilityRepository
{
    Task AddObservabilityEventsAsync(
        IReadOnlyList<ObservabilityEvent> events,
        CancellationToken cancellationToken = default);

    Task AddPageViewsAsync(
        IReadOnlyList<PageView> pageViews,
        CancellationToken cancellationToken = default);

    Task AddAccessEventsAsync(
        IReadOnlyList<AccessEvent> accessEvents,
        CancellationToken cancellationToken = default);

    Task<int> PurgeObservabilityEventsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);

    Task<int> PurgePageViewsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);

    Task<int> PurgeAccessEventsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);

    Task<PagedObservabilityEventsDto> QueryObservabilityEventsAsync(
        ObservabilityEventQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedPageViewsDto> QueryPageViewsAsync(
        PageViewQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedAccessEventsDto> QueryAccessEventsAsync(
        AccessEventQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedPayslipAccessLogDto> QueryPayslipAccessLogAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? targetPersonId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ObservabilitySummaryDto> GetSummaryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<int> CountErrorsSinceAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    Task<ObservabilityMetricsDto> GetMetricsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        TimeSpan bucketSize,
        CancellationToken cancellationToken = default);

    Task<ObservabilityTimelineDto> GetTimelineAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}