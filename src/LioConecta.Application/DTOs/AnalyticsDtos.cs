namespace LioConecta.Application.DTOs;

public sealed record AnalyticsTrendPointDto(string Label, int Value);

public sealed record AnalyticsServiceSliceDto(string Label, int Value, string Color);

public sealed record AnalyticsDepartmentDto(string Name, int ActiveUsers, int Engagement);

public sealed record AnalyticsTopItemDto(
    string Title,
    string Meta,
    int Value,
    string Href,
    string Mod);

public sealed record AnalyticsSnapshotDto(
    string Period,
    int ActivePeople,
    int ActiveUsersInPeriod,
    int FeedPosts,
    int FeedComments,
    int FeedReactions,
    int Comunicados,
    int ComunicadoReads,
    int ActiveGroups,
    int GroupMembers,
    int GroupPosts,
    int Notifications,
    int ServiceRequests,
    int Documents,
    int MoodChecks,
    IReadOnlyList<AnalyticsTrendPointDto> ActivityTrend,
    IReadOnlyList<AnalyticsServiceSliceDto> ServiceBreakdown,
    IReadOnlyList<AnalyticsDepartmentDto> DepartmentEngagement,
    IReadOnlyList<AnalyticsTopItemDto> TopContent);

public sealed record AnalyticsDashboardDto(
    int ActiveUsers,
    int FeedPosts,
    int FeedReactions,
    int FeedComments,
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
