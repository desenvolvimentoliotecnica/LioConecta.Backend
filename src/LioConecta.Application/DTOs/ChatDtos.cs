namespace LioConecta.Application.DTOs;

public sealed record ChatConversationDto(
    Guid Id,
    string? Title,
    PersonSummaryDto? LastMessageAuthor,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);

public sealed record ChatMessageDto(
    Guid Id,
    PersonSummaryDto Author,
    string Text,
    DateTimeOffset CreatedAt);

public sealed record SendMessageRequest(string Text);
