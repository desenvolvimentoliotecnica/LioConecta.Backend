using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class BenefitRepository(AppDbContext db) : IBenefitRepository
{
    public async Task<IReadOnlyList<EmployeeBenefit>> ListAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var items = await db.EmployeeBenefits
            .AsNoTracking()
            .Where(b => b.PersonId == personId && b.IsActive)
            .OrderByDescending(b => b.Featured)
            .ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<EmployeeBenefit?> GetByKeyAsync(
        Guid personId,
        string benefitKey,
        CancellationToken cancellationToken = default) =>
        db.EmployeeBenefits
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.PersonId == personId && b.BenefitKey == benefitKey && b.IsActive,
                cancellationToken);

    public Task<int> CountActiveAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.EmployeeBenefits.CountAsync(
            b => b.PersonId == personId && b.IsActive,
            cancellationToken);
}
