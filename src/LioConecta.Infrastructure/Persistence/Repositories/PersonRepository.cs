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
}
