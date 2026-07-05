using LioConecta.Application.Common.Audit;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IAuditService
{
    void Queue(PendingAuditEvent pendingEvent);

    void RecordHttp(
        string method,
        string path,
        int statusCode,
        int durationMs,
        Guid? actorId,
        string? requestBody);

    void RecordEntityChange(
        EntityStateKind state,
        string entityType,
        string entityId,
        Guid? actorId,
        string? detailsJson);

    Task FlushAsync(CancellationToken cancellationToken = default);

    Task<PagedAuditEventsDto> QueryAsync(AuditEventQuery query, CancellationToken cancellationToken = default);

    Task<AuditEventSummaryDto> GetSummaryAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctActionsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctTargetTypesAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit = 50,
        CancellationToken cancellationToken = default);
}

public enum EntityStateKind
{
    Added,
    Modified,
    Deleted,
}
