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
    int PollsCreated,
    int PollVotes,
    int ActivePolls,
    int PollsClosed,
    int PollParticipationRate,
    int PollAvgVotesPerPoll,
    IReadOnlyList<AnalyticsTrendPointDto> PollActivityTrend,
    IReadOnlyList<AnalyticsTopItemDto> TopPolls,
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

public sealed record SearchSystemHitDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Category);

public sealed record SearchFeedHitDto(
    Guid Id,
    string ContentPreview,
    string? AuthorName,
    DateTimeOffset CreatedAt);

public sealed record SearchUniLioHitDto(
    Guid Id,
    string Title,
    string? Description,
    string InstructorName,
    string Area);

public sealed record SearchRamalHitDto(
    Guid Id,
    string Name,
    string Extension,
    string Department,
    string? Title,
    string? Email);

public sealed record SearchKnowledgeHitDto(
    string Id,
    string Title,
    string Summary,
    string Category,
    string Url);

public sealed record SearchCalendarHitDto(
    Guid Id,
    string Title,
    string? Location,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt);

public sealed record SearchBookmarkHitDto(
    Guid Id,
    string Title,
    string Excerpt,
    string Href,
    string Kind);

public sealed record SearchResultDto(
    IReadOnlyList<PersonSummaryDto> People,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<ComunicadoListItemDto> Comunicados,
    IReadOnlyList<GroupDto> Groups,
    IReadOnlyList<SearchSystemHitDto> Systems,
    IReadOnlyList<SearchFeedHitDto> FeedPosts,
    IReadOnlyList<SearchUniLioHitDto> UniLioCourses,
    IReadOnlyList<SearchRamalHitDto> Ramais,
    IReadOnlyList<SearchKnowledgeHitDto> Knowledge,
    IReadOnlyList<SearchCalendarHitDto> CalendarEvents,
    IReadOnlyList<SearchBookmarkHitDto> Bookmarks);

public sealed record ActivityDto(
    Guid Id,
    string Type,
    string Title,
    string? Description,
    string? Href,
    DateTimeOffset OccurredAt);
