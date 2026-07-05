using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record FeedPostDto(
    Guid Id,
    PostType Type,
    string Content,
    PersonSummaryDto Author,
    DateTimeOffset CreatedAt,
    bool IsPinned,
    IReadOnlyDictionary<string, object?> Metadata,
    int CommentCount,
    int ReactionCount,
    string? ViewerReaction,
    IReadOnlyList<CommentDto> Comments);

public sealed record CommentDto(
    Guid Id,
    string Text,
    PersonSummaryDto Author,
    DateTimeOffset CreatedAt);

public sealed record CreatePostRequest(
    PostType Type,
    string Content,
    IReadOnlyDictionary<string, object?>? Metadata);

public sealed record CreateCommentRequest(string Text);

public sealed record ReactionRequest(string ReactionType);
