using LioConecta.Application.Common;
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

    public Task<EmployeeBenefit?> GetByKeyIncludingInactiveAsync(
        Guid personId,
        string benefitKey,
        CancellationToken cancellationToken = default) =>
        db.EmployeeBenefits
            .FirstOrDefaultAsync(
                b => b.PersonId == personId && b.BenefitKey == benefitKey,
                cancellationToken);

    public Task<EmployeeBenefit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.EmployeeBenefits
            .Include(b => b.Person)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<int> CountActiveAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.EmployeeBenefits.CountAsync(
            b => b.PersonId == personId && b.IsActive,
            cancellationToken);

    public async Task<IReadOnlyList<EmployeeBenefit>> ListForManagementAsync(
        Guid? personId,
        string? departmentId,
        string? catalogKey,
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = db.EmployeeBenefits
            .AsNoTracking()
            .Include(b => b.Person)!
            .ThenInclude(p => p!.Department)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        if (personId is Guid pid)
        {
            query = query.Where(b => b.PersonId == pid);
        }

        if (!string.IsNullOrWhiteSpace(catalogKey))
        {
            var key = catalogKey.Trim();
            query = query.Where(b => b.BenefitKey == key);
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            var cat = category.Trim();
            query = query.Where(b => b.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(b =>
                EF.Functions.ILike(b.Title, pattern) ||
                EF.Functions.ILike(b.BenefitKey, pattern) ||
                (b.Person != null && EF.Functions.ILike(b.Person.Name, pattern)));
        }

        var items = await query
            .OrderBy(b => b.Person!.Name)
            .ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(departmentId) || departmentId == "all")
        {
            return items;
        }

        return items
            .Where(b => PersonSlugHelper.DepartmentIdFromName(
                PersonDepartmentHelper.GetName(b.Person)) == departmentId)
            .ToList();
    }

    public Task AddAsync(EmployeeBenefit entity, CancellationToken cancellationToken = default)
    {
        db.EmployeeBenefits.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
