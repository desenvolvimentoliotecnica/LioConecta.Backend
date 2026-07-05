using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class SearchRepository(AppDbContext db) : ISearchRepository
{
    public async Task<IReadOnlyList<Person>> SearchPeopleAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var pattern = $"%{normalized}%";
        return await db.People
            .AsNoTracking()
            .Where(p => p.IsActive &&
                (EF.Functions.ILike(p.Name, pattern) ||
                 EF.Functions.ILike(p.Email, pattern) ||
                 EF.Functions.ILike(p.Slug, pattern)))
            .OrderBy(p => p.Name)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentMetadata>> SearchDocumentsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var pattern = $"%{normalized}%";
        return await db.Documents
            .AsNoTracking()
            .Where(d =>
                EF.Functions.ILike(d.Title, pattern) ||
                EF.Functions.ILike(d.Category, pattern))
            .OrderByDescending(d => d.ModifiedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Comunicado>> SearchComunicadosAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var pattern = $"%{normalized}%";
        return await db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .Where(c =>
                EF.Functions.ILike(c.Title, pattern) ||
                (c.Excerpt != null && EF.Functions.ILike(c.Excerpt, pattern)))
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }
}
