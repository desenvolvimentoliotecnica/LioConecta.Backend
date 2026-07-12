using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class WikiRepository(AppDbContext db) : IWikiRepository
{
    public async Task<IReadOnlyList<WikiArticle>> ListAsync(
        string? query,
        string? category,
        WikiArticleStatus? status,
        bool includeUnpublished,
        CancellationToken cancellationToken = default)
    {
        var q = db.WikiArticles
            .Include(a => a.Author)
            .AsNoTracking()
            .AsQueryable();

        if (!includeUnpublished)
        {
            q = q.Where(a => a.Status == WikiArticleStatus.Published);
        }
        else if (status.HasValue)
        {
            q = q.Where(a => a.Status == status.Value);
        }
        else
        {
            q = q.Where(a => a.Status != WikiArticleStatus.Archived);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLowerInvariant();
            q = q.Where(a => a.Category == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            q = q.Where(a =>
                a.Title.ToLower().Contains(normalized) ||
                a.Summary.ToLower().Contains(normalized) ||
                a.Category.ToLower().Contains(normalized));
        }

        return await q
            .OrderByDescending(a => a.UpdatedAt)
            .ThenBy(a => a.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(string Category, int Count)>> GetCategoryCountsAsync(
        bool includeUnpublished,
        CancellationToken cancellationToken = default)
    {
        var q = db.WikiArticles.AsNoTracking().AsQueryable();
        if (!includeUnpublished)
        {
            q = q.Where(a => a.Status == WikiArticleStatus.Published);
        }
        else
        {
            q = q.Where(a => a.Status != WikiArticleStatus.Archived);
        }

        var rows = await q
            .GroupBy(a => a.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Category, r.Count)).ToList();
    }

    public Task<WikiArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.WikiArticles
            .Include(a => a.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<WikiArticle?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.WikiArticles
            .Include(a => a.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);

    public Task<WikiArticle?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.WikiArticles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var q = db.WikiArticles.AsNoTracking().Where(a => a.Slug == slug);
        if (excludeId.HasValue)
        {
            q = q.Where(a => a.Id != excludeId.Value);
        }

        return q.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(WikiArticle article, CancellationToken cancellationToken = default) =>
        await db.WikiArticles.AddAsync(article, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
