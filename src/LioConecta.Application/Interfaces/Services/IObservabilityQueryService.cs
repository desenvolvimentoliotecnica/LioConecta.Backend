using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IObservabilityQueryService
{
    Task<ObservabilitySummaryDto> GetSummaryAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    Task<PagedObservabilityEventsDto> QueryErrorsAsync(
        ObservabilityEventQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedPageViewsDto> QueryPageViewsAsync(
        PageViewQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedAccessEventsDto> QueryAccessEventsAsync(
        AccessEventQuery query,
        CancellationToken cancellationToken = default);

    Task<ObservabilityMetricsDto> GetMetricsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? period,
        CancellationToken cancellationToken = default);

    Task<ObservabilityTimelineDto> InvestigateAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
