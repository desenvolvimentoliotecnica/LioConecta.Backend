namespace LioConecta.Application.DTOs;

public sealed record UniLioBootstrapDto(
    bool Enabled,
    IReadOnlyList<string> AllowedRoles,
    IReadOnlyList<string> AllowedEmails);

public sealed record UniLioMetaDto(
    string Persona,
    IReadOnlyList<string> Areas,
    IReadOnlyList<string> Departments,
    IReadOnlyList<string> ContentTypes,
    IReadOnlyList<string> Skills);

public sealed record UniLioQuery(
    string? Area = null,
    string? Department = null,
    string? ContentType = null,
    string? Status = null,
    string? Search = null,
    string? Period = null,
    int Page = 1,
    int PageSize = 20);

public sealed record UniLioKpiDto(
    string Id,
    string Label,
    string Value,
    string Delta,
    string Trend,
    string Icon);

public sealed record UniLioCourseSummaryDto(
    Guid Id,
    string SeedKey,
    string Title,
    string Description,
    string ContentType,
    int DurationMinutes,
    bool IsMandatory,
    string Area,
    string Department,
    decimal Rating,
    string InstructorName,
    string? ThumbnailUrl,
    string? ExternalUrl,
    string? Provider,
    string Status,
    int? ProgressPct,
    string? EnrollmentStatus,
    IReadOnlyList<string> SkillNames,
    IReadOnlyList<UniLioIntegrationLinkDto> Integrations);

public sealed record UniLioIntegrationLinkDto(
    string SourceType,
    string SourceKey,
    string Label);

public sealed record UniLioModuleDto(
    Guid Id,
    int SortOrder,
    string Title,
    string ContentType,
    string? ContentUrl,
    int DurationMinutes,
    string? ArticleHtml,
    string? QuizJson,
    int? QuizPassingScore,
    bool IsCompleted);

public sealed record UniLioCourseDetailDto(
    Guid Id,
    string SeedKey,
    string Title,
    string Description,
    string ContentType,
    int DurationMinutes,
    bool IsMandatory,
    string Area,
    string Department,
    decimal Rating,
    string InstructorName,
    string? ThumbnailUrl,
    string? ExternalUrl,
    string? Provider,
    string Status,
    int ProgressPct,
    string EnrollmentStatus,
    IReadOnlyList<UniLioModuleDto> Modules,
    IReadOnlyList<string> SkillNames,
    IReadOnlyList<UniLioIntegrationLinkDto> Integrations);

public sealed record UniLioPathSummaryDto(
    Guid Id,
    string SeedKey,
    string Title,
    string Description,
    int CourseCount,
    int ProgressPct,
    int CompletedCourses);

public sealed record UniLioPathDetailDto(
    Guid Id,
    string SeedKey,
    string Title,
    string Description,
    int ProgressPct,
    IReadOnlyList<UniLioCourseSummaryDto> Courses);

public sealed record UniLioAlertDto(
    string Id,
    string Severity,
    string Title,
    string Description,
    string Link);

public sealed record UniLioRecommendationDto(
    Guid CourseId,
    string Title,
    string Reason,
    string Area,
    int DurationMinutes,
    string ContentType);

public sealed record UniLioDashboardDto(
    IReadOnlyList<UniLioKpiDto> Kpis,
    UniLioPathSummaryDto? ActivePath,
    IReadOnlyList<UniLioAlertDto> Alerts,
    IReadOnlyList<UniLioCourseSummaryDto> NextSteps,
    IReadOnlyList<UniLioRecommendationDto> TopRecommendations);

public sealed record UniLioCatalogPageDto(
    IReadOnlyList<UniLioCourseSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record UniLioPathsDto(
    IReadOnlyList<UniLioPathSummaryDto> Items);

public sealed record UniLioProgressDto(
    Guid CourseId,
    int ProgressPct,
    string Status,
    bool CourseCompleted);

public sealed record UniLioAssessmentSummaryDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    int PassingScore,
    int? LastScore,
    bool? LastPassed,
    DateTimeOffset? LastAttemptedAt,
    string Status);

public sealed record UniLioAssessmentsDto(
    IReadOnlyList<UniLioAssessmentSummaryDto> Pending,
    IReadOnlyList<UniLioAssessmentSummaryDto> History);

public sealed record UniLioAssessmentSubmitRequest(
    IReadOnlyDictionary<string, string> Answers);

public sealed record UniLioAssessmentResultDto(
    Guid AttemptId,
    int Score,
    bool Passed,
    bool CertificateIssued,
    string? CertificateCode);

public sealed record UniLioCertificateDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string CertificateCode,
    DateTimeOffset IssuedAt,
    string Area);

public sealed record UniLioCertificatesDto(
    IReadOnlyList<UniLioCertificateDto> Items);

public sealed record UniLioComplianceItemDto(
    Guid CourseId,
    string Title,
    string Area,
    int ProgressPct,
    string Status,
    DateTimeOffset? DueDate,
    bool IsOverdue);

public sealed record UniLioComplianceDto(
    IReadOnlyList<UniLioComplianceItemDto> Items,
    int CompletedCount,
    int PendingCount,
    int OverdueCount);

public sealed record UniLioCommunityPostDto(
    Guid Id,
    string AuthorName,
    string? AuthorAvatarUrl,
    string? CourseTitle,
    string Body,
    int LikesCount,
    DateTimeOffset CreatedAt);

public sealed record UniLioCommunityPageDto(
    IReadOnlyList<UniLioCommunityPostDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record UniLioRecommendationsDto(
    IReadOnlyList<UniLioRecommendationDto> Items);

public sealed record UniLioEventDto(
    Guid Id,
    string Title,
    string EventType,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? InstructorName,
    int MaxAttendees,
    int RegisteredCount,
    bool IsRegistered,
    string? MeetingUrl);

public sealed record UniLioEventsDto(
    IReadOnlyList<UniLioEventDto> Items);

public sealed record UniLioSkillLevelDto(
    Guid SkillId,
    string Name,
    string Category,
    int CurrentLevel,
    int TargetLevel,
    IReadOnlyList<string> RelatedCourseTitles);

public sealed record UniLioSkillsDto(
    IReadOnlyList<UniLioSkillLevelDto> Items);

public sealed record UniLioTeamMemberDto(
    Guid PersonId,
    string Name,
    string Department,
    int EnrolledCount,
    int CompletedCount,
    int MandatoryPending,
    int AvgProgressPct);

public sealed record UniLioManagerTeamDto(
    IReadOnlyList<UniLioTeamMemberDto> Members,
    int TotalMembers,
    int AvgCompletionPct);

public sealed record UniLioInstructorCourseDto(
    Guid CourseId,
    string Title,
    string Area,
    int EnrolledCount,
    int CompletedCount,
    decimal AvgRating,
    string Status);

public sealed record UniLioInstructorCoursesDto(
    IReadOnlyList<UniLioInstructorCourseDto> Items);

public sealed record UniLioReportMetricDto(
    string Label,
    string Value,
    string Delta);

public sealed record UniLioReportsDto(
    IReadOnlyList<UniLioReportMetricDto> Metrics,
    IReadOnlyList<UniLioCourseSummaryDto> TopCourses,
    IReadOnlyList<UniLioComplianceItemDto> ComplianceGaps);
