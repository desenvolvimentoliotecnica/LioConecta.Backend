using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record AuditEventDto(
    Guid Id,
    Guid CorrelationId,
    Guid TransactionId,
    AuditSource Source,
    string Action,
    Guid? ActorId,
    string? ActorName,
    string TargetType,
    string TargetId,
    string? HttpMethod,
    string? Path,
    int? StatusCode,
    int? DurationMs,
    string? DetailsJson,
    DateTimeOffset CreatedAt);

public sealed record AuditEventQuery(
    string? Action = null,
    Guid? ActorId = null,
    string? TargetType = null,
    Guid? CorrelationId = null,
    AuditSource? Source = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? HttpStatus = null,
    int Page = 1,
    int PageSize = 25);

public sealed record PagedAuditEventsDto(
    IReadOnlyList<AuditEventDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record AuditEventSummaryDto(
    int TotalCount,
    int HttpCount,
    int EntityCount,
    int ErrorCount,
    int UniqueActors,
    int UniqueActions);
