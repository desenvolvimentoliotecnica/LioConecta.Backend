using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<PagedResult<Notification>> GetPageAsync(
        Guid personId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cursorCreatedAt, cursorId) = CursorHelper.Parse(request.Cursor);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.Notifications
            .AsNoTracking()
            .Where(n => n.PersonId == personId)
            .AsQueryable();

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(n =>
                n.CreatedAt < cursorCreatedAt.Value ||
                (n.CreatedAt == cursorCreatedAt.Value && n.Id.CompareTo(cursorId.Value) < 0));
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items = items.Take(limit).ToList();
        }

        var nextCursor = hasMore && items.Count > 0
            ? CursorHelper.Encode(items[^1].CreatedAt, items[^1].Id)
            : null;

        return PagedResult<Notification>.FromItems(items, nextCursor, hasMore);
    }

    public Task<int> GetUnreadCountAsync(Guid personId, CancellationToken cancellationToken = default) =>
        db.Notifications.CountAsync(n => n.PersonId == personId && !n.IsRead, cancellationToken);

    public async Task MarkAsReadAsync(
        Guid notificationId,
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.PersonId == personId, cancellationToken);

        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.Notifications
            .Where(n => n.PersonId == personId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.UpdatedAt, now),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetActivePersonsAsync(CancellationToken cancellationToken = default) =>
        await db.People
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(
        IReadOnlyList<Notification> notifications,
        CancellationToken cancellationToken = default)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync(cancellationToken);
    }
}
