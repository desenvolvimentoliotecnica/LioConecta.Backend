using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class PayslipRepository(AppDbContext db) : IPayslipRepository
{
    public Task<Payslip?> GetByCompetenceAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        db.Payslips
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.PersonId == personId && p.Year == year && p.Month == month,
                cancellationToken);

    public async Task<IReadOnlyList<Payslip>> ListAsync(
        Guid personId,
        int? year,
        int? month,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = db.Payslips.AsNoTracking().Where(p => p.PersonId == personId);

        if (year is not null)
        {
            query = query.Where(p => p.Year == year);
        }

        if (month is not null)
        {
            query = query.Where(p => p.Month == month);
        }

        return await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<Payslip?> GetLatestAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.Payslips
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.Payslips.CountAsync(p => p.PersonId == personId, cancellationToken);

    public Task<IncomeStatement?> GetIncomeStatementAsync(
        Guid personId,
        int year,
        CancellationToken cancellationToken = default) =>
        db.IncomeStatements
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.PersonId == personId && i.Year == year, cancellationToken);
}
