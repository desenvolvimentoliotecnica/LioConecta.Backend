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
    ICurrentUserService currentUserService) : IFeedService
{
    public async Task<PagedResult<FeedPostDto>> GetFeedAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var page = await feedRepository.GetFeedPageAsync(request, cancellationToken);
        var items = page.Items.Select(p => FeedMapper.ToDto(p, viewerId)).ToList();
        return PagedResult<FeedPostDto>.FromItems(items, page.NextCursor, page.HasMore);
    }

    public async Task<FeedPostDto?> GetPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var post = await feedRepository.GetByIdAsync(id, cancellationToken);
        return post is null ? null : FeedMapper.ToDto(post, viewerId, includeComments: true);
    }

    public async Task<FeedPostDto> CreatePostAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
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

    public async Task<CommentDto> AddCommentAsync(
        Guid postId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var post = await feedRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw new KeyNotFoundException($"Post {postId} was not found.");

        var now = DateTimeOffset.UtcNow;
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            AuthorId = authorId,
            Text = request.Text.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await feedRepository.AddCommentAsync(comment, cancellationToken);
        return FeedMapper.ToCommentDto(comment);
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
