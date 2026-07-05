using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task AddRangeAsync(IReadOnlyList<AuditEvent> events, CancellationToken cancellationToken = default);

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
