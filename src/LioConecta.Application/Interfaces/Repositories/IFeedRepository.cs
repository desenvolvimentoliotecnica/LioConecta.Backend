using LioConecta.Application.Common;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IFeedRepository
{
    Task<PagedResult<FeedPost>> GetFeedPageAsync(CursorPageRequest request, CancellationToken cancellationToken = default);

    Task<FeedPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddPostAsync(FeedPost post, CancellationToken cancellationToken = default);

    Task AddCommentAsync(Comment comment, CancellationToken cancellationToken = default);

    Task<Reaction?> GetReactionAsync(Guid postId, Guid personId, CancellationToken cancellationToken = default);

    Task AddReactionAsync(Reaction reaction, CancellationToken cancellationToken = default);

    Task RemoveReactionAsync(Reaction reaction, CancellationToken cancellationToken = default);

    Task<Poll?> GetPollByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);

    Task AddPollVoteAsync(PollVote vote, CancellationToken cancellationToken = default);

    Task<bool> HasVotedOnPollAsync(Guid pollId, Guid personId, CancellationToken cancellationToken = default);

    Task<Celebration?> GetCelebrationByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedPost>> GetNewsPostsAsync(int limit, CancellationToken cancellationToken = default);
}
