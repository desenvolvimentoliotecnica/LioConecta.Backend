using LioConecta.Application.Common.Observability;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class ObservabilityRepository(AppDbContext db) : IObservabilityRepository
{
    private const short ErrorSeverityThreshold = 4;

    public async Task AddObservabilityEventsAsync(
        IReadOnlyList<ObservabilityEvent> events,
        CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        db.ObservabilityEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPageViewsAsync(
        IReadOnlyList<PageView> pageViews,
        CancellationToken cancellationToken = default)
    {
        if (pageViews.Count == 0)
        {
            return;
        }

        db.PageViews.AddRange(pageViews);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAccessEventsAsync(
        IReadOnlyList<AccessEvent> accessEvents,
        CancellationToken cancellationToken = default)
    {
        if (accessEvents.Count == 0)
        {
            return;
        }

        db.AccessEvents.AddRange(accessEvents);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> PurgeObservabilityEventsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.ObservabilityEvents
            .Where(e => e.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> PurgePageViewsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.PageViews
            .Where(e => e.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> PurgeAccessEventsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.AccessEvents
            .Where(e => e.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<PagedObservabilityEventsDto> QueryObservabilityEventsAsync(
        ObservabilityEventQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var minSeverity = query.MinSeverity ?? ErrorSeverityThreshold;

        var filtered = db.ObservabilityEvents.AsNoTracking()
            .Where(e => e.Severity >= minSeverity);

        filtered = ApplyObservabilityDateRange(filtered, query.From, query.To);

        if (!string.IsNullOrWhiteSpace(query.EventName))
        {
            filtered = filtered.Where(e => e.EventName == query.EventName);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await filtered
            .Include(e => e.User)
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ObservabilityEventListItemDto(
                e.Id,
                e.OccurredAt,
                e.EventType,
                e.EventName,
                e.Severity,
                e.UserId,
                e.User != null ? e.User.Name : null,
                e.CorrelationId,
                e.RouteTemplate,
                e.MetadataJson))
            .ToListAsync(cancellationToken);

        return new PagedObservabilityEventsDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<PagedPageViewsDto> QueryPageViewsAsync(
        PageViewQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filtered = db.PageViews.AsNoTracking().AsQueryable();
        filtered = ApplyPageViewDateRange(filtered, query.From, query.To);

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            filtered = filtered.Where(v => v.Module == query.Module);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await filtered
            .Include(v => v.User)
            .OrderByDescending(v => v.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new PageViewListItemDto(
                v.Id,
                v.OccurredAt,
                v.UserId,
                v.User != null ? v.User.Name : null,
                v.SessionId,
                v.CorrelationId,
                v.PageName,
                v.RouteTemplate,
                v.Module,
                v.ReferrerTemplate,
                v.DurationMs))
            .ToListAsync(cancellationToken);

        return new PagedPageViewsDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<PagedAccessEventsDto> QueryAccessEventsAsync(
        AccessEventQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filtered = db.AccessEvents.AsNoTracking().AsQueryable();
        filtered = ApplyAccessDateRange(filtered, query.From, query.To);

        if (!string.IsNullOrWhiteSpace(query.Result))
        {
            filtered = filtered.Where(e => e.Result == query.Result);
        }

        if (!string.IsNullOrWhiteSpace(query.EventName))
        {
            filtered = filtered.Where(e => e.EventName == query.EventName);
        }

        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await filtered
            .Include(e => e.User)
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AccessEventListItemDto(
                e.Id,
                e.OccurredAt,
                e.EventType,
                e.EventName,
                e.UserId,
                e.User != null ? e.User.Name : null,
                e.UsernameSnapshot,
                e.CorrelationId,
                e.Resource,
                e.Action,
                e.Result,
                e.ReasonCode))
            .ToListAsync(cancellationToken);

        return new PagedAccessEventsDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<ObservabilitySummaryDto> GetSummaryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var errorsLast24h = await CountErrorsSinceAsync(DateTimeOffset.UtcNow.AddHours(-24), cancellationToken);

        var observabilityQuery = db.ObservabilityEvents.AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to);
        var pageViewQuery = db.PageViews.AsNoTracking()
            .Where(v => v.OccurredAt >= from && v.OccurredAt <= to);
        var accessQuery = db.AccessEvents.AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to);
        var auditHttpQuery = db.AuditEvents.AsNoTracking()
            .Where(e => e.Source == AuditSource.HttpRequest && e.CreatedAt >= from && e.CreatedAt <= to);

        var observabilityEvents = await observabilityQuery.CountAsync(cancellationToken);
        var pageViews = await pageViewQuery.CountAsync(cancellationToken);
        var accessEvents = await accessQuery.CountAsync(cancellationToken);
        var accessDenied = await accessQuery.CountAsync(
            e => e.Result == AccessEventResults.Denied,
            cancellationToken);
        var authFailures = await accessQuery.CountAsync(
            e => e.EventName == ObservabilityEventNames.Authentication.LoginFailed ||
                 e.EventName == ObservabilityEventNames.Authentication.AnonymousBlocked ||
                 e.EventName == ObservabilityEventNames.Authentication.SessionExpired,
            cancellationToken);

        var httpTotal = await auditHttpQuery.CountAsync(cancellationToken);
        var httpErrors = await auditHttpQuery.CountAsync(
            e => e.StatusCode.HasValue && e.StatusCode >= 400,
            cancellationToken);
        var httpErrorRate = httpTotal == 0 ? 0 : Math.Round(httpErrors * 100.0 / httpTotal, 2);

        var durations = await auditHttpQuery
            .Where(e => e.DurationMs.HasValue)
            .Select(e => e.DurationMs!.Value)
            .ToListAsync(cancellationToken);
        var p95 = ComputeP95(durations);

        var rangeMinutes = Math.Max(1, (to - from).TotalMinutes);
        var requestsPerMinute = Math.Round(httpTotal / rangeMinutes, 2);

        var dailyActiveUsers = await pageViewQuery
            .Where(v => v.UserId.HasValue)
            .Select(v => v.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var topModule = await pageViewQuery
            .GroupBy(v => v.Module)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync(cancellationToken);

        var topPage = await pageViewQuery
            .GroupBy(v => v.RouteTemplate)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync(cancellationToken);

        return new ObservabilitySummaryDto(
            errorsLast24h,
            httpErrorRate,
            p95,
            requestsPerMinute,
            dailyActiveUsers,
            pageViews,
            accessDenied,
            authFailures,
            topModule,
            topPage,
            observabilityEvents,
            accessEvents);
    }

    public Task<int> CountErrorsSinceAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default) =>
        db.ObservabilityEvents.AsNoTracking()
            .CountAsync(e => e.OccurredAt >= since && e.Severity >= ErrorSeverityThreshold, cancellationToken);

    public async Task<ObservabilityMetricsDto> GetMetricsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        TimeSpan bucketSize,
        CancellationToken cancellationToken = default)
    {
        var auditRows = await db.AuditEvents.AsNoTracking()
            .Where(e => e.Source == AuditSource.HttpRequest && e.CreatedAt >= from && e.CreatedAt <= to)
            .Select(e => new { e.CreatedAt, e.StatusCode, e.DurationMs })
            .ToListAsync(cancellationToken);

        var bucketMinutes = Math.Max(1, bucketSize.TotalMinutes);
        var buckets = new SortedDictionary<DateTimeOffset, List<(int? StatusCode, int? DurationMs)>>();

        foreach (var row in auditRows)
        {
            var bucketStart = FloorToBucket(row.CreatedAt, bucketSize);
            if (!buckets.TryGetValue(bucketStart, out var list))
            {
                list = [];
                buckets[bucketStart] = list;
            }

            list.Add((row.StatusCode, row.DurationMs));
        }

        var rpmSeries = new List<ObservabilityMetricPointDto>();
        var errorRateSeries = new List<ObservabilityMetricPointDto>();
        var p95Series = new List<ObservabilityMetricPointDto>();

        foreach (var (timestamp, entries) in buckets)
        {
            var total = entries.Count;
            var errors = entries.Count(e => e.StatusCode is >= 400);
            var durations = entries.Where(e => e.DurationMs.HasValue).Select(e => e.DurationMs!.Value).ToList();

            rpmSeries.Add(new ObservabilityMetricPointDto(timestamp, Math.Round(total / bucketMinutes, 2)));
            errorRateSeries.Add(new ObservabilityMetricPointDto(
                timestamp,
                total == 0 ? 0 : Math.Round(errors * 100.0 / total, 2)));
            p95Series.Add(new ObservabilityMetricPointDto(timestamp, ComputeP95(durations) ?? 0));
        }

        return new ObservabilityMetricsDto(rpmSeries, errorRateSeries, p95Series);
    }

    public async Task<ObservabilityTimelineDto> GetTimelineAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var items = new List<ObservabilityTimelineItemDto>();

        var pageViews = await db.PageViews.AsNoTracking()
            .Where(v => v.CorrelationId == correlationId)
            .Select(v => new ObservabilityTimelineItemDto(
                v.OccurredAt,
                "page_view",
                v.PageName,
                v.RouteTemplate,
                v.Id))
            .ToListAsync(cancellationToken);
        items.AddRange(pageViews);

        var accessEvents = await db.AccessEvents.AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .Select(e => new ObservabilityTimelineItemDto(
                e.OccurredAt,
                "access_event",
                e.EventName,
                e.Resource ?? e.Result,
                e.Id))
            .ToListAsync(cancellationToken);
        items.AddRange(accessEvents);

        var observabilityEvents = await db.ObservabilityEvents.AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .Select(e => new ObservabilityTimelineItemDto(
                e.OccurredAt,
                "observability_event",
                e.EventName,
                e.RouteTemplate ?? e.EventType,
                e.Id))
            .ToListAsync(cancellationToken);
        items.AddRange(observabilityEvents);

        var auditEvents = await db.AuditEvents.AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .Select(e => new ObservabilityTimelineItemDto(
                e.CreatedAt,
                "audit_event",
                e.Action,
                e.Path ?? e.TargetType,
                e.Id))
            .ToListAsync(cancellationToken);
        items.AddRange(auditEvents);

        return new ObservabilityTimelineDto(
            correlationId,
            items.OrderBy(i => i.OccurredAt).ToList());
    }

    private static IQueryable<ObservabilityEvent> ApplyObservabilityDateRange(
        IQueryable<ObservabilityEvent> query,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= to);
        }

        return query;
    }

    private static IQueryable<PageView> ApplyPageViewDateRange(
        IQueryable<PageView> query,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from.HasValue)
        {
            query = query.Where(v => v.OccurredAt >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(v => v.OccurredAt <= to);
        }

        return query;
    }

    private static IQueryable<AccessEvent> ApplyAccessDateRange(
        IQueryable<AccessEvent> query,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= to);
        }

        return query;
    }

    private static int? ComputeP95(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var ordered = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(ordered.Count * 0.95) - 1;
        index = Math.Clamp(index, 0, ordered.Count - 1);
        return ordered[index];
    }

    private static DateTimeOffset FloorToBucket(DateTimeOffset value, TimeSpan bucketSize)
    {
        var ticks = bucketSize.Ticks;
        if (ticks <= 0)
        {
            return value;
        }

        var flooredTicks = value.UtcTicks / ticks * ticks;
        return new DateTimeOffset(flooredTicks, TimeSpan.Zero);
    }
}
