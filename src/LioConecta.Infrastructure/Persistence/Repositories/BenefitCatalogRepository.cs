using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class BenefitCatalogRepository(AppDbContext db) : IBenefitCatalogRepository
{
    public async Task<IReadOnlyList<BenefitCatalog>> ListAsync(
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = db.BenefitCatalogs.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(item => item.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            var cat = category.Trim();
            query = query.Where(item => item.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Title, pattern) ||
                EF.Functions.ILike(item.CatalogKey, pattern) ||
                EF.Functions.ILike(item.Provider, pattern) ||
                EF.Functions.ILike(item.Desc, pattern));
        }

        return await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<BenefitCatalog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BenefitCatalogs.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<BenefitCatalog?> GetByKeyAsync(string catalogKey, CancellationToken cancellationToken = default) =>
        db.BenefitCatalogs.FirstOrDefaultAsync(item => item.CatalogKey == catalogKey, cancellationToken);

    public Task<bool> KeyExistsAsync(string catalogKey, Guid? excludeId, CancellationToken cancellationToken = default) =>
        db.BenefitCatalogs.AsNoTracking().AnyAsync(
            item => item.CatalogKey == catalogKey && (excludeId == null || item.Id != excludeId),
            cancellationToken);

    public Task AddAsync(BenefitCatalog entity, CancellationToken cancellationToken = default)
    {
        db.BenefitCatalogs.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
