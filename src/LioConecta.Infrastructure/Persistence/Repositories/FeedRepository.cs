using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class FeedRepository(AppDbContext db) : IFeedRepository
{
    public async Task<PagedResult<FeedPost>> GetFeedPageAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var (cursorCreatedAt, cursorId) = CursorHelper.Parse(request.Cursor);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = db.FeedPosts
            .Include(p => p.Author)
            .Include(p => p.Comments).ThenInclude(c => c.Author)
            .Include(p => p.Reactions).ThenInclude(r => r.Person)
            .AsNoTracking()
            .AsQueryable();

        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(p =>
                p.CreatedAt < cursorCreatedAt.Value ||
                (p.CreatedAt == cursorCreatedAt.Value && p.Id.CompareTo(cursorId.Value) < 0));
        }

        var items = await query
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
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

        return PagedResult<FeedPost>.FromItems(items, nextCursor, hasMore);
    }

    public Task<FeedPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.FeedPosts
            .Include(p => p.Author)
            .Include(p => p.Comments).ThenInclude(c => c.Author)
            .Include(p => p.Reactions).ThenInclude(r => r.Person)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddPostAsync(FeedPost post, CancellationToken cancellationToken = default)
    {
        db.FeedPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPostWithPollAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default)
    {
        db.FeedPosts.Add(post);
        db.Polls.Add(poll);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Poll>> GetPollsByPostIdsAsync(
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken = default)
    {
        if (postIds.Count == 0)
        {
            return [];
        }

        return await db.Polls
            .Include(p => p.Options).ThenInclude(o => o.Votes)
            .AsNoTracking()
            .Where(p => postIds.Contains(p.PostId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddCommentAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Reaction?> GetReactionAsync(
        Guid postId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.Reactions.FirstOrDefaultAsync(
            r => r.PostId == postId && r.PersonId == personId,
            cancellationToken);

    public async Task AddReactionAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        db.Reactions.Add(reaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveReactionAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        db.Reactions.Remove(reaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Poll?> GetPollByPostIdAsync(Guid postId, CancellationToken cancellationToken = default) =>
        db.Polls
            .Include(p => p.Options).ThenInclude(o => o.Votes)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PostId == postId, cancellationToken);

    public async Task AddPollVoteAsync(PollVote vote, CancellationToken cancellationToken = default)
    {
        db.PollVotes.Add(vote);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasVotedOnPollAsync(
        Guid pollId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.PollVotes
            .AnyAsync(
                v => v.PersonId == personId &&
                     db.PollOptions.Any(o => o.Id == v.PollOptionId && o.PollId == pollId),
                cancellationToken);

    public Task<Celebration?> GetCelebrationByPostIdAsync(
        Guid postId,
        CancellationToken cancellationToken = default) =>
        db.Celebrations
            .Include(c => c.CelebratedPerson)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PostId == postId, cancellationToken);

    public Task<IReadOnlyList<FeedPost>> GetNewsPostsAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
        db.FeedPosts
            .Include(p => p.Author)
            .AsNoTracking()
            .Where(p => p.Type == PostType.News)
            .OrderByDescending(p => p.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<FeedPost>)t.Result, cancellationToken);
}
