namespace LioConecta.Application.DTOs;

public sealed record PollOptionDto(
    Guid Id,
    string Text,
    int VoteCount,
    int SortOrder,
    bool IsSelectedByViewer);

public sealed record PollDto(
    Guid Id,
    Guid PostId,
    string Question,
    DateTimeOffset? EndsAt,
    bool HasViewerVoted,
    IReadOnlyList<PollOptionDto> Options);

public sealed record VotePollRequest(Guid OptionId);

public sealed record CelebrationDto(
    Guid Id,
    Guid PostId,
    PersonSummaryDto CelebratedPerson,
    string Message);

public sealed record NewsItemDto(
    Guid Id,
    string Title,
    string Excerpt,
    string? HeroImageUrl,
    PersonSummaryDto Author,
    DateTimeOffset PublishedAt,
    string? Href);
