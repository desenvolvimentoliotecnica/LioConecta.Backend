using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Services;

public sealed class ObservabilityQueryService(IObservabilityRepository repository) : IObservabilityQueryService
{
    private static readonly TimeSpan DefaultRange = TimeSpan.FromDays(30);

    public async Task<ObservabilitySummaryDto> GetSummaryAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ResolveRange(from, to, DefaultRange);
        return await repository.GetSummaryAsync(start, end, cancellationToken);
    }

    public Task<PagedObservabilityEventsDto> QueryErrorsAsync(
        ObservabilityEventQuery query,
        CancellationToken cancellationToken = default) =>
        repository.QueryObservabilityEventsAsync(query, cancellationToken);

    public Task<PagedPageViewsDto> QueryPageViewsAsync(
        PageViewQuery query,
        CancellationToken cancellationToken = default) =>
        repository.QueryPageViewsAsync(query, cancellationToken);

    public Task<PagedAccessEventsDto> QueryAccessEventsAsync(
        AccessEventQuery query,
        CancellationToken cancellationToken = default) =>
        repository.QueryAccessEventsAsync(query, cancellationToken);

    public async Task<ObservabilityMetricsDto> GetMetricsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? period,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ResolveMetricsRange(from, to, period);
        var bucketSize = ResolveBucketSize(start, end, period);
        return await repository.GetMetricsAsync(start, end, bucketSize, cancellationToken);
    }

    public Task<ObservabilityTimelineDto> InvestigateAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default) =>
        repository.GetTimelineAsync(correlationId, cancellationToken);

    private static (DateTimeOffset From, DateTimeOffset To) ResolveRange(
        DateTimeOffset? from,
        DateTimeOffset? to,
        TimeSpan defaultSpan)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end.Subtract(defaultSpan);
        return (start, end);
    }

    private static (DateTimeOffset From, DateTimeOffset To) ResolveMetricsRange(
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? period)
    {
        if (from.HasValue || to.HasValue)
        {
            return ResolveRange(from, to, TimeSpan.FromHours(24));
        }

        return period?.Trim().ToLowerInvariant() switch
        {
            "7d" => ResolveRange(null, null, TimeSpan.FromDays(7)),
            "30d" => ResolveRange(null, null, TimeSpan.FromDays(30)),
            "90d" => ResolveRange(null, null, TimeSpan.FromDays(90)),
            _ => ResolveRange(null, null, TimeSpan.FromHours(24)),
        };
    }

    private static TimeSpan ResolveBucketSize(DateTimeOffset from, DateTimeOffset to, string? period)
    {
        var span = to - from;

        if (span <= TimeSpan.FromDays(2))
        {
            return TimeSpan.FromHours(1);
        }

        if (span <= TimeSpan.FromDays(14))
        {
            return TimeSpan.FromHours(6);
        }

        return period?.Trim().ToLowerInvariant() switch
        {
            "90d" => TimeSpan.FromDays(1),
            _ => TimeSpan.FromHours(12),
        };
    }
}
