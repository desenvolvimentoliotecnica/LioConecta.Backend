using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class PersonRepository(AppDbContext db) : IPersonRepository
{
    public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.People
            .Include(p => p.Department)
            .Include(p => p.Manager)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Person?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.People
            .Include(p => p.Department)
            .Include(p => p.Manager)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Person>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim();
        var queryable = db.People
            .AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Manager)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            var pattern = $"%{normalized}%";
            queryable = queryable.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.Email, pattern) ||
                EF.Functions.ILike(p.Slug, pattern) ||
                (p.Title != null && EF.Functions.ILike(p.Title, pattern)) ||
                (p.Dept != null && EF.Functions.ILike(p.Dept, pattern)));
        }

        return await queryable
            .OrderBy(p => p.Name)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetOrgChartPeopleAsync(CancellationToken cancellationToken = default) =>
        await db.People
            .Include(p => p.Manager)
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Person>> GetDirectoryPeopleAsync(
        string? query,
        string? departmentId,
        CancellationToken cancellationToken = default)
    {
        var queryable = db.People
            .AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Manager)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            queryable = queryable.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.Email, pattern) ||
                (p.Title != null && EF.Functions.ILike(p.Title, pattern)) ||
                (p.Dept != null && EF.Functions.ILike(p.Dept, pattern)) ||
                (p.Department != null && EF.Functions.ILike(p.Department.Name, pattern)));
        }

        var people = await queryable
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(departmentId) || departmentId == "all")
        {
            return people;
        }

        return people
            .Where(p => Application.Common.PersonSlugHelper.DepartmentIdFromName(
                Application.Common.PersonDepartmentHelper.GetName(p)) == departmentId)
            .ToList();
    }

    public async Task<IReadOnlyList<Person>> GetPeersAsync(
        Guid personId,
        Guid managerId,
        CancellationToken cancellationToken = default) =>
        await db.People
            .AsNoTracking()
            .Include(p => p.Department)
            .Where(p => p.IsActive && p.ManagerId == managerId && p.Id != personId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Person>> GetDirectReportsAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        await db.People
            .AsNoTracking()
            .Include(p => p.Department)
            .Where(p => p.IsActive && p.ManagerId == personId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Person person, CancellationToken cancellationToken = default)
    {
        db.People.Add(person);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Person person, CancellationToken cancellationToken = default)
    {
        db.People.Update(person);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByAzureObjectIdsAsync(
        IEnumerable<Guid> objectIds,
        CancellationToken cancellationToken = default)
    {
        var ids = objectIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await db.People
            .AsNoTracking()
            .Where(p => p.AzureAdObjectId != null && ids.Contains(p.AzureAdObjectId.Value))
            .ToListAsync(cancellationToken);
    }

    public Task<Person?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return db.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByEmailsAsync(
        IEnumerable<string> emails,
        CancellationToken cancellationToken = default)
    {
        var normalized = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
        {
            return [];
        }

        return await db.People
            .AsNoTracking()
            .Where(p => p.IsActive && normalized.Contains(p.Email.ToLower()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await db.People
            .AsNoTracking()
            .Where(p => p.IsActive && idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }
}
