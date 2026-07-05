using LioConecta.Application.Common;
using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IFeedService
{
    Task<PagedResult<FeedPostDto>> GetFeedAsync(CursorPageRequest request, CancellationToken cancellationToken = default);

    Task<FeedPostDto?> GetPostAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FeedPostDto> CreatePostAsync(CreatePostRequest request, CancellationToken cancellationToken = default);

    Task<CommentDto> AddCommentAsync(Guid postId, CreateCommentRequest request, CancellationToken cancellationToken = default);

    Task ReactAsync(Guid postId, ReactionRequest request, CancellationToken cancellationToken = default);

    Task<PollDto?> GetPollAsync(Guid postId, CancellationToken cancellationToken = default);

    Task VotePollAsync(Guid postId, VotePollRequest request, CancellationToken cancellationToken = default);

    Task<CelebrationDto?> GetCelebrationAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsItemDto>> GetNewsAsync(int limit = 10, CancellationToken cancellationToken = default);
}
