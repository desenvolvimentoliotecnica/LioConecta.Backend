using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class LeaveRepository(AppDbContext db) : ILeaveRepository
{
    private const string VacationServiceKey = "solicitar-ferias";

    public Task<EmployeeLeaveBalance?> GetBalanceAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.EmployeeLeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.PersonId == personId, cancellationToken);

    public Task<DateTimeOffset?> GetBalanceSyncedAtAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.EmployeeLeaveBalances
            .AsNoTracking()
            .Where(b => b.PersonId == personId)
            .Select(b => b.SyncedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpsertBalanceAsync(EmployeeLeaveBalance balance, CancellationToken cancellationToken = default)
    {
        var existing = await db.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.PersonId == balance.PersonId, cancellationToken);

        if (existing is null)
        {
            db.EmployeeLeaveBalances.Add(balance);
        }
        else
        {
            existing.AvailableDays = balance.AvailableDays;
            existing.AcquiredDays = balance.AcquiredDays;
            existing.ScheduledDays = balance.ScheduledDays;
            existing.ExpiredDays = balance.ExpiredDays;
            existing.BancoHorasBalanceHours = balance.BancoHorasBalanceHours;
            existing.NextScheduledStart = balance.NextScheduledStart;
            existing.NextScheduledEnd = balance.NextScheduledEnd;
            existing.BreakdownJson = balance.BreakdownJson;
            existing.DataSource = balance.DataSource;
            existing.SyncedAt = balance.SyncedAt;
            existing.UpdatedAt = balance.UpdatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

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

    public async Task<IReadOnlyList<LeaveRecord>> ListRequestsAsync(
        Guid personId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = await db.LeaveRecords
            .AsNoTracking()
            .Where(r => r.PersonId == personId && r.ServiceKey == VacationServiceKey)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<LeaveRecord?> GetRecordByIdAsync(Guid recordId, CancellationToken cancellationToken = default) =>
        db.LeaveRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);

    public Task<int> CountPendingAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.LeaveRecords.CountAsync(
            r => r.PersonId == personId
                 && r.ServiceKey == VacationServiceKey
                 && (r.Status == "pending" || r.RmSyncStatus == "pending_rm_sync"),
            cancellationToken);

    public async Task AddRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default)
    {
        db.LeaveRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default)
    {
        db.LeaveRecords.Update(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRecord>> ListPendingWriteBackAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = await db.LeaveRecords
            .Where(r => r.ServiceKey == VacationServiceKey && r.RmSyncStatus == "pending_rm_sync")
            .OrderBy(r => r.CreatedAt)
            .Take(Math.Clamp(limit, 1, 50))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task UpsertRmRecordAsync(LeaveRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(record.RmExternalId))
        {
            await AddRecordAsync(record, cancellationToken);
            return;
        }

        var existing = await db.LeaveRecords
            .FirstOrDefaultAsync(
                r => r.PersonId == record.PersonId && r.RmExternalId == record.RmExternalId,
                cancellationToken);

        if (existing is null)
        {
            db.LeaveRecords.Add(record);
        }
        else
        {
            existing.Title = record.Title;
            existing.Status = record.Status;
            existing.StartDate = record.StartDate;
            existing.EndDate = record.EndDate;
            existing.Days = record.Days;
            existing.DetailsJson = record.DetailsJson;
            existing.DataSource = record.DataSource;
            existing.SyncedAt = record.SyncedAt;
            existing.UpdatedAt = record.UpdatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
