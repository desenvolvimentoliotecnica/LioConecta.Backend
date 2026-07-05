using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class ComunicadoRepository(AppDbContext db) : IComunicadoRepository
{
    public async Task<PagedResult<Comunicado>> GetPageAsync(
        ComunicadoKind? kind,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cursorCreatedAt, cursorId) = CursorHelper.Parse(request.Cursor);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .AsQueryable();

        if (kind.HasValue)
        {
            query = query.Where(c => c.Kind == kind.Value);
        }

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(c =>
                (c.PublishedAt ?? c.CreatedAt) < cursorCreatedAt.Value ||
                ((c.PublishedAt ?? c.CreatedAt) == cursorCreatedAt.Value && c.Id.CompareTo(cursorId.Value) < 0));
        }

        var items = await query
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var last = items.Count > 0 ? items[^1] : null;
        var nextCursor = hasMore && last is not null
            ? CursorHelper.Encode(last.PublishedAt ?? last.CreatedAt, last.Id)
            : null;

        return PagedResult<Comunicado>.FromItems(items, nextCursor, hasMore);
    }

    public Task<Comunicado?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task MarkAsReadAsync(
        Guid comunicadoId,
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.ComunicadoReads.AnyAsync(
            r => r.ComunicadoId == comunicadoId && r.PersonId == personId,
            cancellationToken);

        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        db.ComunicadoReads.Add(new ComunicadoRead
        {
            Id = Guid.NewGuid(),
            ComunicadoId = comunicadoId,
            PersonId = personId,
            ReadAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsReadAsync(
        Guid comunicadoId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.ComunicadoReads.AnyAsync(
            r => r.ComunicadoId == comunicadoId && r.PersonId == personId,
            cancellationToken);

    public async Task<IReadOnlyList<Comunicado>> SearchAsync(
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
