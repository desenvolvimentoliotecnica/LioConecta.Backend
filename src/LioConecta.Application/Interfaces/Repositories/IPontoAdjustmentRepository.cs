using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IPontoAdjustmentRepository
{
    Task AddAsync(PontoAdjustmentRecord record, CancellationToken cancellationToken = default);

    Task<PontoAdjustmentRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<PontoAdjustmentRecord?> GetWithPersonAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PontoAdjustmentRecord>> ListByPersonAsync(
        Guid personId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PontoAdjustmentRecord>> ListManagementAsync(
        IReadOnlyList<Guid>? personIds,
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(PontoAdjustmentRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PontoAdjustmentRecord>> ListPendingWriteBackAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
