namespace LioConecta.Application.DTOs;

public sealed record ObservabilityEventListItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string EventType,
    string EventName,
    short Severity,
    Guid? UserId,
    string? UserName,
    Guid CorrelationId,
    string? RouteTemplate,
    string? MetadataJson);

public sealed record PageViewListItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid? UserId,
    string? UserName,
    Guid SessionId,
    Guid CorrelationId,
    string PageName,
    string RouteTemplate,
    string Module,
    string? ReferrerTemplate,
    int? DurationMs);

public sealed record AccessEventListItemDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string EventType,
    string EventName,
    Guid? UserId,
    string? UserName,
    string? UsernameSnapshot,
    Guid CorrelationId,
    string? Resource,
    string? Action,
    string Result,
    string? ReasonCode);

public sealed record ObservabilityEventQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? EventName = null,
    short? MinSeverity = null,
    int Page = 1,
    int PageSize = 25);

public sealed record PageViewQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Module = null,
    int Page = 1,
    int PageSize = 25);

public sealed record AccessEventQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Result = null,
    string? EventName = null,
    int Page = 1,
    int PageSize = 25);

public sealed record PagedObservabilityEventsDto(
    IReadOnlyList<ObservabilityEventListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PagedPageViewsDto(
    IReadOnlyList<PageViewListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PagedAccessEventsDto(
    IReadOnlyList<AccessEventListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ObservabilitySummaryDto(
    int ErrorsLast24h,
    double HttpErrorRate,
    int? P95LatencyMs,
    double RequestsPerMinute,
    int DailyActiveUsers,
    int PageViews,
    int AccessDenied,
    int AuthFailures,
    string? TopModule,
    string? TopPage,
    int ObservabilityEvents,
    int AccessEvents);

public sealed record ObservabilityMetricPointDto(
    DateTimeOffset Timestamp,
    double Value);

public sealed record ObservabilityMetricsDto(
    IReadOnlyList<ObservabilityMetricPointDto> RequestsPerMinute,
    IReadOnlyList<ObservabilityMetricPointDto> ErrorRate,
    IReadOnlyList<ObservabilityMetricPointDto> P95LatencyMs);

public sealed record ObservabilityTimelineItemDto(
    DateTimeOffset OccurredAt,
    string Source,
    string Label,
    string? Detail,
    Guid ReferenceId);

public sealed record ObservabilityTimelineDto(
    Guid CorrelationId,
    IReadOnlyList<ObservabilityTimelineItemDto> Items);
