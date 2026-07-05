namespace LioConecta.Application.DTOs;

public sealed record TelemetryIngestResultDto(int Accepted, int Rejected);

public sealed record TelemetryEventsBatchDto(
    Guid SessionId,
    Guid CorrelationId,
    IReadOnlyList<TelemetryEventIngestDto> Events);

public sealed record TelemetryEventIngestDto(
    string EventType,
    string EventName,
    DateTimeOffset OccurredAt,
    short Severity,
    Dictionary<string, object?>? Properties);

public sealed record TelemetryPageViewsBatchDto(
    Guid SessionId,
    Guid CorrelationId,
    IReadOnlyList<TelemetryPageViewIngestDto> Views);

public sealed record TelemetryPageViewIngestDto(
    DateTimeOffset OccurredAt,
    string PageName,
    string RouteTemplate,
    string Module,
    string? ReferrerTemplate,
    int? DurationMs);
