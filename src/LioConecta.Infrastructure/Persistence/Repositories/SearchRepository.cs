using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class SearchRepository(AppDbContext db) : ISearchRepository
{
    public async Task<IReadOnlyList<Person>> SearchPeopleAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.People
            .AsNoTracking()
            .Where(p => p.IsActive &&
                (EF.Functions.ILike(p.Name, pattern) ||
                 EF.Functions.ILike(p.Email, pattern) ||
                 EF.Functions.ILike(p.Slug, pattern)))
            .OrderBy(p => p.Name)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentMetadata>> SearchDocumentsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.Documents
            .AsNoTracking()
            .Where(d =>
                EF.Functions.ILike(d.Title, pattern) ||
                EF.Functions.ILike(d.Category, pattern))
            .OrderByDescending(d => d.ModifiedAt)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Comunicado>> SearchComunicadosAsync(
        string query,
        int limit,
        Guid? viewerDepartmentId,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.Comunicados
            .Include(c => c.Author)
            .AsNoTracking()
            .Where(c => c.Status == ComunicadoStatus.Published)
            .Where(c =>
                c.AudienceType == ComunicadoAudienceType.All ||
                (viewerDepartmentId.HasValue &&
                 EF.Functions.ILike(c.AudienceDepartmentIdsJson, $"%{viewerDepartmentId.Value}%")))
            .Where(c =>
                EF.Functions.ILike(c.Title, pattern) ||
                (c.Excerpt != null && EF.Functions.ILike(c.Excerpt, pattern)))
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> SearchGroupsAsync(
        string query,
        int limit,
        Guid viewerPersonId,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Approver)
            .Include(g => g.Members)
            .Include(g => g.Posts)
            .Include(g => g.Topics)
            .AsNoTracking()
            .Where(g => g.Status == GroupStatus.Active)
            .Where(g =>
                !g.IsPrivate ||
                g.Members.Any(m => m.PersonId == viewerPersonId))
            .Where(g =>
                EF.Functions.ILike(g.Name, pattern) ||
                (g.Description != null && EF.Functions.ILike(g.Description, pattern)))
            .OrderBy(g => g.Name)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalSystem>> SearchSystemsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.PortalSystems
            .AsNoTracking()
            .Where(s => s.IsActive &&
                (EF.Functions.ILike(s.Name, pattern) ||
                 EF.Functions.ILike(s.Slug, pattern) ||
                 (s.Description != null && EF.Functions.ILike(s.Description, pattern)) ||
                 EF.Functions.ILike(s.Category, pattern)))
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedPost>> SearchFeedPostsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        return await db.FeedPosts
            .Include(p => p.Author)
            .AsNoTracking()
            .Where(p => !p.IsDeleted &&
                (p.ScheduledAt == null || p.ScheduledAt <= now) &&
                EF.Functions.ILike(p.Content, pattern))
            .OrderByDescending(p => p.CreatedAt)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UniLioCourse>> SearchUniLioCoursesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.UniLioCourses
            .AsNoTracking()
            .Where(c =>
                (c.Status == UniLioCourseStatuses.Published || c.Status == UniLioCourseStatuses.Active) &&
                (EF.Functions.ILike(c.Title, pattern) ||
                 EF.Functions.ILike(c.Description, pattern) ||
                 EF.Functions.ILike(c.InstructorName, pattern) ||
                 EF.Functions.ILike(c.Area, pattern)))
            .OrderBy(c => c.Title)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PhoneExtension>> SearchRamaisAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.PhoneExtensions
            .AsNoTracking()
            .Where(r => r.IsActive &&
                (EF.Functions.ILike(r.Name, pattern) ||
                 EF.Functions.ILike(r.Extension, pattern) ||
                 EF.Functions.ILike(r.Department, pattern) ||
                 (r.Title != null && EF.Functions.ILike(r.Title, pattern)) ||
                 (r.Email != null && EF.Functions.ILike(r.Email, pattern))))
            .OrderBy(r => r.Name)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarEvent>> SearchCalendarEventsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.CalendarEvents
            .AsNoTracking()
            .Where(e =>
                EF.Functions.ILike(e.Title, pattern) ||
                (e.Location != null && EF.Functions.ILike(e.Location, pattern)))
            .OrderByDescending(e => e.StartAt)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookmarkCatalogItem>> SearchBookmarksAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = BuildPattern(query);
        if (pattern is null)
        {
            return [];
        }

        return await db.BookmarkCatalogItems
            .AsNoTracking()
            .Where(b => b.IsActive &&
                (EF.Functions.ILike(b.Title, pattern) ||
                 EF.Functions.ILike(b.Excerpt, pattern) ||
                 EF.Functions.ILike(b.Href, pattern)))
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Title)
            .Take(ClampLimit(limit))
            .ToListAsync(cancellationToken);
    }

    public Task<Guid?> GetPersonDepartmentIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.People
            .AsNoTracking()
            .Where(p => p.Id == personId)
            .Select(p => p.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

    private static string? BuildPattern(string query)
    {
        var normalized = query.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : $"%{normalized}%";
    }

    private static int ClampLimit(int limit) => Math.Clamp(limit, 1, 100);
}
