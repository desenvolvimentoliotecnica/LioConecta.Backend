using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface ILeaveRepository
{
    Task<EmployeeLeaveBalance?> GetBalanceAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveRecord>> ListRecordsAsync(
        Guid personId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAsync(Guid personId, CancellationToken cancellationToken = default);

    Task AddRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default);
}
