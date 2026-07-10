using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class PontoAdjustmentRepository(AppDbContext db) : IPontoAdjustmentRepository
{
    public async Task AddAsync(PontoAdjustmentRecord record, CancellationToken cancellationToken = default)
    {
        db.PontoAdjustmentRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<PontoAdjustmentRecord?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken = default) =>
        db.PontoAdjustmentRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);

    public Task<PontoAdjustmentRecord?> GetWithPersonAsync(Guid recordId, CancellationToken cancellationToken = default) =>
        db.PontoAdjustmentRecords
            .AsNoTracking()
            .Include(r => r.Person)
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);

    public async Task<IReadOnlyList<PontoAdjustmentRecord>> ListByPersonAsync(
        Guid personId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await db.PontoAdjustmentRecords
            .AsNoTracking()
            .Where(r => r.PersonId == personId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PontoAdjustmentRecord>> ListManagementAsync(
        IReadOnlyList<Guid>? personIds,
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var queryable = db.PontoAdjustmentRecords
            .AsNoTracking()
            .Include(r => r.Person)
            .AsQueryable();

        if (personIds is not null)
        {
            queryable = queryable.Where(r => personIds.Contains(r.PersonId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            queryable = queryable.Where(r => r.Status.ToLower() == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            queryable = queryable.Where(r =>
                EF.Functions.ILike(r.Title, pattern)
                || EF.Functions.ILike(r.Reason, pattern)
                || (r.Person != null && EF.Functions.ILike(r.Person.Name, pattern))
                || (r.Person != null && EF.Functions.ILike(r.Person.Email, pattern))
                || (r.Person != null
                    && r.Person.EmployeeId != null
                    && EF.Functions.ILike(r.Person.EmployeeId, pattern)));
        }

        return await queryable
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }
}
