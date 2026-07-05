using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class ChatRepository(AppDbContext db) : IChatRepository
{
    public Task<IReadOnlyList<ChatConversation>> GetConversationsAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.ChatConversations
            .Include(c => c.Messages).ThenInclude(m => m.Author)
            .Include(c => c.CreatedBy)
            .AsNoTracking()
            .Where(c => c.CreatedById == personId ||
                        c.Messages.Any(m => m.AuthorId == personId))
            .OrderByDescending(c => c.Messages.Max(m => (DateTimeOffset?)m.CreatedAt) ?? c.CreatedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<ChatConversation>)t.Result, cancellationToken);

    public Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ChatConversations
            .Include(c => c.CreatedBy)
            .Include(c => c.Messages).ThenInclude(m => m.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<ChatMessage>> GetMessagesPageAsync(
        Guid conversationId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cursorCreatedAt, cursorId) = CursorHelper.Parse(request.Cursor);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.ChatMessages
            .Include(m => m.Author)
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .AsQueryable();

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(m =>
                m.CreatedAt < cursorCreatedAt.Value ||
                (m.CreatedAt == cursorCreatedAt.Value && m.Id.CompareTo(cursorId.Value) < 0));
        }

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
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

        return PagedResult<ChatMessage>.FromItems(items, nextCursor, hasMore);
    }

    public async Task AddConversationAsync(
        ChatConversation conversation,
        CancellationToken cancellationToken = default)
    {
        db.ChatConversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
    }
}
