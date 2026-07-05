using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class LeaveRepository(AppDbContext db) : ILeaveRepository
{
    public Task<EmployeeLeaveBalance?> GetBalanceAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.EmployeeLeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.PersonId == personId, cancellationToken);

    public async Task<IReadOnlyList<LeaveRecord>> ListRecordsAsync(
        Guid personId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = await db.LeaveRecords
            .AsNoTracking()
            .Where(r => r.PersonId == personId)
            .OrderByDescending(r => r.StartDate ?? DateOnly.FromDateTime(r.CreatedAt.Date))
            .ThenByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountPendingAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.LeaveRecords.CountAsync(
            r => r.PersonId == personId && r.Status == "pending",
            cancellationToken);

    public async Task AddRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default)
    {
        db.LeaveRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }
}
