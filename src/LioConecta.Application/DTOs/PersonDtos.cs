using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record MeDto(
    Guid Id,
    string Slug,
    string Name,
    string Email,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    IReadOnlyList<UserRole> Roles,
    IReadOnlyList<EffectivePermissionDto> Permissions = null!,
    string? SubjectType = null,
    bool IsTestUser = false);

public sealed record PersonSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    string? Location,
    string? ManagerSlug,
    bool IsActive,
    DateOnly? BirthDate = null,
    DateOnly? HireDate = null);

public sealed record BirthdayPersonDto(
    Guid Id,
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    string? Location,
    string? ManagerSlug,
    bool IsActive,
    DateOnly? BirthDate,
    bool AlreadyCongratulatedByMe);

public sealed record PersonDirectoryEntryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string Email,
    string? TeamsUpn,
    string? DepartmentName,
    string? Location,
    string? ManagerSlug,
    bool IsActive,
    DateOnly? HireDate = null,
    string? Phone = null);

public sealed record PersonDirectoryDepartmentDto(
    string Id,
    string Name,
    int Count,
    IReadOnlyList<PersonDirectoryEntryDto> People);

public sealed record PersonDirectoryDto(
    DateTimeOffset? SyncedAtUtc,
    int Total,
    IReadOnlyList<PersonDirectoryDepartmentDto> Departments);

public sealed record PersonSkillDto(
    string Name,
    int Level,
    int Endorsements);

public sealed record PersonLanguageDto(
    string Name,
    string Level);

public sealed record UpdateProfileAboutRequest(string? AboutMe);

public sealed record UpdateProfileSkillsRequest(
    IReadOnlyList<PersonSkillDto> Skills);

public sealed record UpdateProfileLanguagesRequest(
    IReadOnlyList<PersonLanguageDto> Languages);

public sealed record UpdateProfileLinksRequest(
    IReadOnlyDictionary<string, string> Links);

public sealed record PersonAvailabilityDto(
    string? WorkModel,
    string? Schedule,
    string? Timezone,
    string? Floor,
    string? Room);

public sealed record UpdateProfilePronounsRequest(string? Pronouns);

public sealed record UpdateProfileAvailabilityRequest(PersonAvailabilityDto Availability);

public sealed record PersonContactRefDto(
    string? Name,
    string? Slug,
    string? Since);

public sealed record UpdateProfileMentorshipRequest(
    PersonContactRefDto? Mentor,
    PersonContactRefDto? Buddy);

public sealed record PersonProjectDto(
    string Name,
    string Role,
    string Since,
    string Status);

public sealed record UpdateProfileProjectsRequest(
    IReadOnlyList<PersonProjectDto> Projects);

public sealed record PersonEducationDto(
    string Period,
    string Degree,
    string Institution,
    string? Note,
    string? Type);

public sealed record UpdateProfileEducationRequest(
    IReadOnlyList<PersonEducationDto> Education);

public sealed record PersonCertificationDto(
    string Name,
    string Issuer,
    string Year);

public sealed record UpdateProfileCertificationsRequest(
    IReadOnlyList<PersonCertificationDto> Certifications);

public sealed record PersonCareerHistoryItemDto(
    string Type,
    string Title,
    string Date,
    string Dept,
    string Note);

public sealed record UpdateProfileCareerHistoryRequest(
    IReadOnlyList<PersonCareerHistoryItemDto> CareerHistory);

public sealed record UpdateProfileAvatarRequest(string? PhotoUrl);

public sealed record PersonProfileDto(
    Guid Id,
    string Slug,
    string? OrgChartId,
    string Name,
    string? Title,
    string Email,
    string? Phone,
    string? Location,
    string? PhotoUrl,
    string? DepartmentName,
    string? ManagerName,
    string? ManagerSlug,
    string? TeamsUpn,
    string? Bio,
    string? Pronouns,
    DateOnly? BirthDate,
    DateOnly? HireDate,
    string? Status,
    IReadOnlyList<string> Tags,
    IReadOnlyList<PersonSkillDto> Skills,
    IReadOnlyDictionary<string, object?>? PersonalData,
    ViewerContext ViewerContext,
    string? GraphPhotoUrl = null,
    string? PortalPhotoUrl = null);

public sealed record PersonHierarchyMemberDto(
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName);

public sealed record PersonHierarchyDto(
    PersonHierarchyMemberDto? Manager,
    IReadOnlyList<PersonHierarchyMemberDto> Chain,
    IReadOnlyList<PersonHierarchyMemberDto> Peers,
    IReadOnlyList<PersonHierarchyMemberDto> DirectReports,
    int DirectReportsCount);

public sealed record OrgChartNodeDto(
    Guid Id,
    string? OrgChartId,
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    Guid? ManagerId,
    IReadOnlyList<string> Tags,
    bool IsOrphan = false,
    string? Email = null,
    string? TeamsUpn = null,
    string? Phone = null,
    string? Location = null,
    DateOnly? HireDate = null);

public sealed record OrgChartDto(
    IReadOnlyList<OrgChartNodeDto> Nodes,
    Guid? RootId,
    int Total,
    IReadOnlyList<Guid> RootIds,
    int OrphanCount,
    DateTimeOffset? SyncedAtUtc,
    IReadOnlyList<OrgChartNodeDto> UnassignedNodes,
    int UnassignedCount);
