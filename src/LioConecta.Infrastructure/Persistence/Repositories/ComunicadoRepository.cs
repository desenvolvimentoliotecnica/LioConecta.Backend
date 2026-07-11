using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class ComunicadoRepository(AppDbContext db) : IComunicadoRepository
{
    public async Task<PagedResult<Comunicado>> GetPageAsync(
        ComunicadoKind? kind,
        bool archivedOnly,
        Guid? viewerDepartmentId,
        bool includeUnpublished,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cursorCreatedAt, cursorId) = CursorHelper.Parse(request.Cursor);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .AsQueryable();

        if (archivedOnly)
        {
            query = query.Where(c => c.Status == ComunicadoStatus.Archived);
        }
        else
        {
            query = query.Where(c => c.Status != ComunicadoStatus.Archived);
            if (!includeUnpublished)
            {
                query = query.Where(c => c.Status == ComunicadoStatus.Published);
            }
        }

        if (kind.HasValue)
        {
            query = query.Where(c => c.Kind == kind.Value);
        }

        if (!includeUnpublished)
        {
            query = query.Where(c =>
                c.AudienceType == ComunicadoAudienceType.All ||
                (viewerDepartmentId.HasValue &&
                 EF.Functions.ILike(c.AudienceDepartmentIdsJson, $"%{viewerDepartmentId.Value}%")));
        }

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            if (archivedOnly)
            {
                query = query.Where(c =>
                    c.ArchivedAt! < cursorCreatedAt.Value ||
                    (c.ArchivedAt == cursorCreatedAt.Value && c.Id.CompareTo(cursorId.Value) < 0));
            }
            else
            {
                query = query.Where(c =>
                    (c.PublishedAt ?? c.CreatedAt) < cursorCreatedAt.Value ||
                    ((c.PublishedAt ?? c.CreatedAt) == cursorCreatedAt.Value && c.Id.CompareTo(cursorId.Value) < 0));
            }
        }

        List<Comunicado> items;
        if (archivedOnly)
        {
            items = await query
                .OrderByDescending(c => c.ArchivedAt)
                .ThenByDescending(c => c.Id)
                .Take(limit + 1)
                .ToListAsync(cancellationToken);
        }
        else
        {
            items = await query
                .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Take(limit + 1)
                .ToListAsync(cancellationToken);
        }

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var last = items.Count > 0 ? items[^1] : null;
        string? nextCursor = null;
        if (hasMore && last is not null)
        {
            nextCursor = archivedOnly
                ? CursorHelper.Encode(last.ArchivedAt ?? last.PublishedAt ?? last.CreatedAt, last.Id)
                : CursorHelper.Encode(last.PublishedAt ?? last.CreatedAt, last.Id);
        }

        return PagedResult<Comunicado>.FromItems(items, nextCursor, hasMore);
    }

    public Task<Comunicado?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Comunicado?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

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

    public async Task<IReadOnlyDictionary<ComunicadoKind, int>> GetActiveCountsByKindAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Comunicados
            .AsNoTracking()
            .Where(c => c.ArchivedAt == null)
            .GroupBy(c => c.Kind)
            .Select(g => new { Kind = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Kind, r => r.Count);
    }

    public Task<int> GetArchivedCountAsync(CancellationToken cancellationToken = default) =>
        db.Comunicados.AsNoTracking().CountAsync(c => c.ArchivedAt != null, cancellationToken);

    public Task<int> GetUnreadUrgentCountAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.Comunicados
            .AsNoTracking()
            .Where(c =>
                c.ArchivedAt == null &&
                c.Kind == ComunicadoKind.Urgente &&
                !db.ComunicadoReads.Any(r => r.ComunicadoId == c.Id && r.PersonId == personId))
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Comunicado>> GetRecentActiveAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .Where(c => c.ArchivedAt == null)
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Take(Math.Clamp(limit, 1, 20))
            .ToListAsync(cancellationToken);
    }

    public Task<Guid?> GetDepartmentIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.People
            .AsNoTracking()
            .Where(p => p.Id == personId)
            .Select(p => p.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Comunicado?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Comunicados
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Comunicado>> GetScheduledDueAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await db.Comunicados
            .Include(c => c.Author)
            .Where(c => c.Status == ComunicadoStatus.Scheduled && c.ScheduledAt <= now)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Comunicado comunicado, CancellationToken cancellationToken = default)
    {
        db.Comunicados.Add(comunicado);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task<bool> HasFeedPostAsync(Guid comunicadoId, CancellationToken cancellationToken = default) =>
        db.FeedPosts.AnyAsync(
            post => post.Type == PostType.Comunicado &&
                    EF.Functions.ILike(post.MetadataJson, $"%{comunicadoId}%"),
            cancellationToken);

    public Task AddFeedPostAsync(
        Comunicado comunicado,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken = default)
    {
        db.FeedPosts.Add(ComunicadoFeedMapper.CreateFeedPost(comunicado, timestamp));
        return Task.CompletedTask;
    }

    public async Task<ComunicadoMetrics> GetMetricsAsync(
        Comunicado comunicado,
        CancellationToken cancellationToken = default)
    {
        var departmentIds = JsonSerializer.Deserialize<Guid[]>(comunicado.AudienceDepartmentIdsJson) ?? [];
        var eligibleQuery = db.People.AsNoTracking().Where(p => p.IsActive);
        if (comunicado.AudienceType == ComunicadoAudienceType.Departments)
        {
            eligibleQuery = eligibleQuery.Where(p =>
                p.DepartmentId.HasValue && departmentIds.Contains(p.DepartmentId.Value));
        }

        var eligible = await eligibleQuery.CountAsync(cancellationToken);
        var readCount = await db.ComunicadoReads
            .AsNoTracking()
            .CountAsync(read => read.ComunicadoId == comunicado.Id, cancellationToken);
        return new ComunicadoMetrics(eligible, readCount);
    }
}
