namespace LioConecta.Application.DTOs;

public sealed record AnalyticsDashboardDto(
    int ActiveUsers,
    int FeedPosts,
    int ServiceRequests,
    int UnreadNotifications,
    int MoodChecks,
    IReadOnlyDictionary<string, int> EventsByType);

public sealed record SearchResultDto(
    IReadOnlyList<PersonSummaryDto> People,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<ComunicadoListItemDto> Comunicados);

public sealed record ActivityDto(
    Guid Id,
    string Type,
    string Title,
    string? Description,
    string? Href,
    DateTimeOffset OccurredAt);
