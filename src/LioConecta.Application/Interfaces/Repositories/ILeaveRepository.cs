using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface ILeaveRepository
{
    Task<EmployeeLeaveBalance?> GetBalanceAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetBalanceSyncedAtAsync(Guid personId, CancellationToken cancellationToken = default);

    Task UpsertBalanceAsync(EmployeeLeaveBalance balance, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRecord>> ListRecordsAsync(
        Guid personId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRecord>> ListRequestsAsync(
        Guid personId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<LeaveRecord?> GetRecordByIdAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<LeaveRecord?> GetRecordWithPersonAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRecord>> ListManagementAsync(
        IReadOnlyList<Guid>? personIds,
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(Guid personId, CancellationToken cancellationToken = default);

    Task AddRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default);

    Task UpdateRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRecord>> ListPendingWriteBackAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task UpsertRmRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default);
}
