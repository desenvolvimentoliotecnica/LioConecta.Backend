using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task<IReadOnlyList<DocumentMetadata>> ListAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        var query = db.Documents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(d => d.Category == category);
        }

        return await query
            .OrderByDescending(d => d.ModifiedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<DocumentMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DocumentMetadata>> SearchAsync(
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
                (d.Description != null && EF.Functions.ILike(d.Description, pattern)) ||
                EF.Functions.ILike(d.Category, pattern))
            .OrderByDescending(d => d.ModifiedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(DocumentMetadata document, CancellationToken cancellationToken = default)
    {
        var existing = await db.Documents
            .FirstOrDefaultAsync(d => d.SharePointItemId == document.SharePointItemId, cancellationToken);

        if (existing is null)
        {
            db.Documents.Add(document);
        }
        else
        {
            existing.Title = document.Title;
            existing.Description = document.Description;
            existing.Category = document.Category;
            existing.MediaType = document.MediaType;
            existing.IsFeatured = document.IsFeatured;
            existing.SeedKey = document.SeedKey;
            existing.SharePointUrl = document.SharePointUrl;
            existing.ModifiedAt = document.ModifiedAt;
            existing.UpdatedAt = document.UpdatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
