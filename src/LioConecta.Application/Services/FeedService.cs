using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class FeedService(
    IFeedRepository feedRepository,
    IAnalyticsRepository analyticsRepository,
    ICurrentUserService currentUserService,
    INotificationService notificationService) : IFeedService
{
    public async Task<PagedResult<FeedPostDto>> GetFeedAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var page = await feedRepository.GetFeedPageAsync(request, cancellationToken);
        var pollPostIds = page.Items
            .Where(p => p.Type == PostType.Poll)
            .Select(p => p.Id)
            .ToList();
        var pollsByPostId = (await feedRepository.GetPollsByPostIdsAsync(pollPostIds, cancellationToken))
            .ToDictionary(p => p.PostId);

        var items = page.Items
            .Select(p =>
            {
                PollDto? pollDto = null;
                if (p.Type == PostType.Poll && pollsByPostId.TryGetValue(p.Id, out var poll))
                {
                    pollDto = FeedMapper.ToPollDto(poll, viewerId);
                }

                return FeedMapper.ToDto(p, viewerId, includeComments: true, pollDto);
            })
            .ToList();

        return PagedResult<FeedPostDto>.FromItems(items, page.NextCursor, page.HasMore);
    }

    public async Task<FeedPostDto?> GetPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var post = await feedRepository.GetByIdAsync(id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        PollDto? pollDto = null;
        if (post.Type == PostType.Poll)
        {
            var poll = await feedRepository.GetPollByPostIdAsync(id, cancellationToken);
            if (poll is not null)
            {
                pollDto = FeedMapper.ToPollDto(poll, viewerId);
            }
        }

        return FeedMapper.ToDto(post, viewerId, includeComments: true, pollDto);
    }

    public async Task<FeedPostDto> CreatePostAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (request.Type == PostType.Poll)
        {
            return await CreatePollPostAsync(request, authorId, now, cancellationToken);
        }

        var post = new FeedPost
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Type = request.Type,
            Content = request.Content.Trim(),
            MetadataJson = JsonMapper.SerializeObjectDictionary(request.Metadata),
            CreatedAt = now,
            UpdatedAt = now
        };

        await feedRepository.AddPostAsync(post, cancellationToken);
        var saved = await feedRepository.GetByIdAsync(post.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Post {post.Id} was not found after save.");
        return FeedMapper.ToDto(saved, authorId);
    }

    private async Task<FeedPostDto> CreatePollPostAsync(
        CreatePostRequest request,
        Guid authorId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var parsed = PollCreateParser.Parse(request.Content, request.Metadata);
        var metadata = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(parsed.HeroImageUrl))
        {
            metadata["heroImageUrl"] = parsed.HeroImageUrl;
        }

        if (parsed.EndsAt is not null)
        {
            metadata["endsAt"] = parsed.EndsAt.Value.ToString("O");
        }

        var postId = Guid.NewGuid();
        var pollId = Guid.NewGuid();
        var post = new FeedPost
        {
            Id = postId,
            AuthorId = authorId,
            Type = PostType.Poll,
            Content = parsed.Question,
            MetadataJson = JsonMapper.SerializeObjectDictionary(metadata),
            CreatedAt = now,
            UpdatedAt = now
        };

        var poll = new Poll
        {
            Id = pollId,
            PostId = postId,
            Question = parsed.Question,
            EndsAt = parsed.EndsAt,
            CreatedAt = now,
            UpdatedAt = now,
            Options = parsed.Options
                .Select((text, index) => new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = pollId,
                    Text = text,
                    SortOrder = index,
                    CreatedAt = now,
                    UpdatedAt = now
                })
                .ToList()
        };

        await feedRepository.AddPostWithPollAsync(post, poll, cancellationToken);

        var saved = await feedRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new InvalidOperationException($"Post {postId} was not found after save.");
        var savedPoll = await feedRepository.GetPollByPostIdAsync(postId, cancellationToken)
            ?? throw new InvalidOperationException($"Poll for post {postId} was not found after save.");
        var pollDto = FeedMapper.ToPollDto(savedPoll, authorId);

        await notificationService.NotifyPollCreatedAsync(saved, savedPoll, cancellationToken);

        return FeedMapper.ToDto(saved, authorId, poll: pollDto);
    }

    public async Task<CommentDto> AddCommentAsync(
        Guid postId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var text = request.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Comment text is required.", nameof(request));
        }

        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var post = await feedRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new KeyNotFoundException($"Post {postId} was not found.");

        var now = DateTimeOffset.UtcNow;
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            AuthorId = authorId,
            Text = text,
            CreatedAt = now,
            UpdatedAt = now
        };

        await feedRepository.AddCommentAsync(comment, cancellationToken);

        var savedPost = await feedRepository.GetByIdAsync(post.Id, cancellationToken);
        var savedComment = savedPost?.Comments.FirstOrDefault(c => c.Id == comment.Id) ?? comment;

        await TrackCommentEventAsync(authorId, post.Id, savedComment.Id, cancellationToken);

        return FeedMapper.ToCommentDto(savedComment);
    }

    private async Task TrackCommentEventAsync(
        Guid personId,
        Guid postId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await analyticsRepository.AddEventAsync(new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            EventType = "FeedPostCommented",
            PersonId = personId,
            MetadataJson = $"{{\"postId\":\"{postId}\",\"commentId\":\"{commentId}\"}}",
            OccurredAt = now,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
    }

    public async Task ReactAsync(
        Guid postId,
        ReactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var post = await feedRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new KeyNotFoundException($"Post {postId} was not found.");

        var existing = await feedRepository.GetReactionAsync(postId, personId, cancellationToken);
        if (existing is not null)
        {
            if (string.Equals(existing.ReactionType, request.ReactionType, StringComparison.OrdinalIgnoreCase))
            {
                await feedRepository.RemoveReactionAsync(existing, cancellationToken);
                await TrackReactionEventAsync("FeedPostUnliked", personId, post.Id, request.ReactionType, cancellationToken);
                return;
            }

            await feedRepository.RemoveReactionAsync(existing, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        await feedRepository.AddReactionAsync(new Reaction
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            PersonId = personId,
            ReactionType = request.ReactionType,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await TrackReactionEventAsync("FeedPostLiked", personId, post.Id, request.ReactionType, cancellationToken);
    }

    private async Task TrackReactionEventAsync(
        string eventType,
        Guid personId,
        Guid postId,
        string reactionType,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await analyticsRepository.AddEventAsync(new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            PersonId = personId,
            MetadataJson = $"{{\"postId\":\"{postId}\",\"reactionType\":\"{reactionType}\"}}",
            OccurredAt = now,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
    }

    public async Task<PollDto?> GetPollAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var poll = await feedRepository.GetPollByPostIdAsync(postId, cancellationToken);
        if (poll is null)
        {
            return null;
        }

        var hasVoted = await feedRepository.HasVotedOnPollAsync(poll.Id, viewerId, cancellationToken);
        var options = poll.Options
            .OrderBy(o => o.SortOrder)
            .Select(o => new PollOptionDto(
                o.Id,
                o.Text,
                o.Votes.Count,
                o.SortOrder,
                hasVoted && o.Votes.Any(v => v.PersonId == viewerId)))
            .ToList();

        return new PollDto(poll.Id, poll.PostId, poll.Question, poll.EndsAt, hasVoted, options);
    }

    public async Task VotePollAsync(
        Guid postId,
        VotePollRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var poll = await feedRepository.GetPollByPostIdAsync(postId, cancellationToken)
            ?? throw new KeyNotFoundException($"Poll for post {postId} was not found.");

        if (poll.EndsAt is not null && poll.EndsAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("This poll is closed.");
        }

        if (await feedRepository.HasVotedOnPollAsync(poll.Id, personId, cancellationToken))
        {
            throw new InvalidOperationException("You have already voted on this poll.");
        }

        var option = poll.Options.FirstOrDefault(o => o.Id == request.OptionId)
            ?? throw new KeyNotFoundException($"Poll option {request.OptionId} was not found.");

        var now = DateTimeOffset.UtcNow;
        await feedRepository.AddPollVoteAsync(new PollVote
        {
            Id = Guid.NewGuid(),
            PollOptionId = option.Id,
            PersonId = personId,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
    }

    public async Task<CelebrationDto?> GetCelebrationAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var celebration = await feedRepository.GetCelebrationByPostIdAsync(postId, cancellationToken);
        if (celebration is null)
        {
            return null;
        }

        return new CelebrationDto(
            celebration.Id,
            celebration.PostId,
            PersonMapper.ToSummary(celebration.CelebratedPerson ?? new Person { Name = "Desconhecido" }),
            celebration.Message);
    }

    public async Task<IReadOnlyList<NewsItemDto>> GetNewsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var posts = await feedRepository.GetNewsPostsAsync(limit, cancellationToken);
        return posts.Select(post =>
        {
            var metadata = JsonMapper.DeserializeObjectDictionary(post.MetadataJson);
            metadata.TryGetValue("title", out var titleObj);
            metadata.TryGetValue("excerpt", out var excerptObj);
            metadata.TryGetValue("heroImageUrl", out var heroObj);
            metadata.TryGetValue("href", out var hrefObj);

            return new NewsItemDto(
                post.Id,
                titleObj?.ToString() ?? post.Content,
                excerptObj?.ToString() ?? string.Empty,
                heroObj?.ToString(),
                PersonMapper.ToSummary(post.Author ?? new Person { Name = "Desconhecido" }),
                post.CreatedAt,
                hrefObj?.ToString());
        }).ToList();
    }
}
