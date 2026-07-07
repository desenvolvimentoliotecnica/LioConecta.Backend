using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class PayslipRepository(AppDbContext db) : IPayslipRepository
{
    public async Task<Payslip?> GetByCompetenceAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var payslips = await db.Payslips
            .AsNoTracking()
            .Where(p => p.PersonId == personId && p.Year == year && p.Month == month)
            .ToListAsync(cancellationToken);

        return payslips
            .OrderBy(p => p.PaymentType == "FOLHA" ? 0 : 1)
            .ThenByDescending(p => p.NroPeriodo)
            .FirstOrDefault();
    }

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
            .ThenBy(p => p.PaymentType == "FOLHA" ? 0 : 1)
            .ThenByDescending(p => p.NroPeriodo)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payslip?> GetLatestAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var latestCompetence = await db.Payslips
            .AsNoTracking()
            .Where(p => p.PersonId == personId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new { p.Year, p.Month })
            .FirstOrDefaultAsync(cancellationToken);

        if (latestCompetence is null)
        {
            return null;
        }

        return await GetByCompetenceAsync(
            personId,
            latestCompetence.Year,
            latestCompetence.Month,
            cancellationToken);
    }

    public Task<int> CountAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.Payslips.CountAsync(p => p.PersonId == personId, cancellationToken);

    public Task<IncomeStatement?> GetIncomeStatementAsync(
        Guid personId,
        int year,
        CancellationToken cancellationToken = default) =>
        db.IncomeStatements
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.PersonId == personId && i.Year == year, cancellationToken);

    public async Task UpsertAsync(Payslip payslip, CancellationToken cancellationToken = default)
    {
        var existing = await db.Payslips.FirstOrDefaultAsync(
            p => p.PersonId == payslip.PersonId
                 && p.Year == payslip.Year
                 && p.Month == payslip.Month
                 && p.PaymentType == payslip.PaymentType,
            cancellationToken);

        if (existing is null)
        {
            db.Payslips.Add(payslip);
        }
        else
        {
            existing.NroPeriodo = payslip.NroPeriodo;
            existing.GrossAmount = payslip.GrossAmount;
            existing.NetAmount = payslip.NetAmount;
            existing.DeductionsTotal = payslip.DeductionsTotal;
            existing.EarningsJson = payslip.EarningsJson;
            existing.DeductionsJson = payslip.DeductionsJson;
            existing.PublishedAt = payslip.PublishedAt;
            existing.SyncedAtUtc = payslip.SyncedAtUtc;
            existing.Source = payslip.Source;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<DateTimeOffset?> GetMaxSyncedAtUtcAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.Payslips
            .AsNoTracking()
            .Where(p => p.PersonId == personId && p.SyncedAtUtc != null)
            .MaxAsync(p => p.SyncedAtUtc, cancellationToken);

    public async Task<int> DeleteWithoutSourceAsync(
        Guid personId,
        string requiredSource,
        CancellationToken cancellationToken = default)
    {
        var stale = await db.Payslips
            .Where(p => p.PersonId == personId && (p.Source == null || p.Source != requiredSource))
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return 0;
        }

        db.Payslips.RemoveRange(stale);
        await db.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    public async Task<int> DeleteBeforeCompetenceAsync(
        Guid personId,
        int fromYear,
        int fromMonth,
        CancellationToken cancellationToken = default)
    {
        var stale = await db.Payslips
            .Where(p => p.PersonId == personId &&
                        (p.Year < fromYear || (p.Year == fromYear && p.Month < fromMonth)))
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return 0;
        }

        db.Payslips.RemoveRange(stale);
        await db.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    public async Task UpsertIncomeStatementAsync(
        IncomeStatement statement,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.IncomeStatements.FirstOrDefaultAsync(
            i => i.PersonId == statement.PersonId && i.Year == statement.Year,
            cancellationToken);

        if (existing is null)
        {
            db.IncomeStatements.Add(statement);
        }
        else
        {
            existing.TotalPaid = statement.TotalPaid;
            existing.TotalWithheld = statement.TotalWithheld;
            existing.LinesJson = statement.LinesJson;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteIncomeStatementsWithoutSourceAsync(
        Guid personId,
        string requiredSource,
        CancellationToken cancellationToken = default)
    {
        // IncomeStatement rows are always RM-backed after sync; remove legacy seed rows
        // identified by empty LinesJson or zero totals with no matching payslip year.
        var payslipYears = await db.Payslips
            .AsNoTracking()
            .Where(p => p.PersonId == personId && p.Source == requiredSource)
            .Select(p => p.Year)
            .Distinct()
            .ToListAsync(cancellationToken);

        var stale = await db.IncomeStatements
            .Where(i => i.PersonId == personId && !payslipYears.Contains(i.Year))
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return 0;
        }

        db.IncomeStatements.RemoveRange(stale);
        await db.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }
}
