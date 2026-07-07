using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record OrgChartSettingsDto(
    bool GovernanceEnabled,
    IReadOnlyList<string> EditAllowedRoles,
    IReadOnlyList<string> EditAllowedEmails,
    IReadOnlyList<string> ViewFullAllowedRoles,
    bool AllowDisplayNameEdit,
    bool AllowReimport,
    bool ShowOverrideBadge,
    DateTimeOffset UpdatedAt,
    Guid? UpdatedById);

public sealed record UpsertOrgChartSettingsRequest(
    bool GovernanceEnabled,
    IReadOnlyList<string> EditAllowedRoles,
    IReadOnlyList<string> EditAllowedEmails,
    IReadOnlyList<string> ViewFullAllowedRoles,
    bool AllowDisplayNameEdit,
    bool AllowReimport,
    bool ShowOverrideBadge);

public sealed record OrgChartPolicyDto(
    bool CanEdit,
    bool CanImport,
    bool CanManageDepartments,
    bool CanViewFull,
    IReadOnlyList<string> AllowedFields,
    bool GovernanceEnabled);

public sealed record GovernedOrgChartNodeDto(
    Guid Id,
    string? OrgChartId,
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    Guid? ManagerId,
    IReadOnlyList<string> Tags,
    bool IsOrphan,
    string? Email,
    string? TeamsUpn,
    string? Phone,
    string? Location,
    DateOnly? HireDate,
    Guid PositionId,
    bool HasManualOverride,
    string? GraphTitle,
    string? GraphDepartmentName,
    string? GraphManagerName,
    Guid? OrgDepartmentId,
    Guid? ManagerPositionId,
    string? ManagerName,
    bool IsVisible);

public sealed record GovernedOrgChartDto(
    IReadOnlyList<GovernedOrgChartNodeDto> Nodes,
    Guid? RootId,
    int Total,
    IReadOnlyList<Guid> RootIds,
    int OrphanCount,
    DateTimeOffset? SyncedAtUtc,
    IReadOnlyList<GovernedOrgChartNodeDto> UnassignedNodes,
    int UnassignedCount);

public sealed record OrgPositionDetailDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string? Title,
    string? DepartmentName,
    Guid? OrgDepartmentId,
    Guid? ManagerPositionId,
    string? ManagerName,
    bool IsVisible,
    int SortOrder,
    bool HasManualOverride,
    OrgPositionSource Source,
    string? GraphTitle,
    string? GraphDepartmentName,
    DateTimeOffset UpdatedAt);

public sealed record UpsertOrgPositionRequest(
    string? Title,
    string? DepartmentName,
    Guid? OrgDepartmentId,
    Guid? ManagerPositionId,
    bool? IsVisible,
    int? SortOrder,
    string? DisplayName);

public sealed record CreateOrgPositionRequest(
    Guid PersonId,
    string? Title,
    string? DepartmentName,
    Guid? OrgDepartmentId,
    Guid? ManagerPositionId,
    bool IsVisible = true,
    int SortOrder = 0);

public sealed record OrgDepartmentDto(
    Guid Id,
    string Name,
    Guid? ParentDepartmentId,
    int SortOrder,
    bool IsActive);

public sealed record UpsertOrgDepartmentRequest(
    string Name,
    Guid? ParentDepartmentId,
    int SortOrder,
    bool IsActive);

public sealed record OrgChartGovernanceSummaryDto(
    int TotalPositions,
    int VisiblePositions,
    int ManualOverrides,
    int TotalDepartments,
    int ActiveDepartments,
    DateTimeOffset? LastImportAt);

public sealed record ImportFromGraphRequest(bool Force);

public sealed record OrgDepartmentMappingDto(
    Guid Id,
    string SourceName,
    Guid? OrgDepartmentId,
    string? OrgDepartmentName,
    int EmployeeCount,
    bool IsActive);

public sealed record UpsertOrgDepartmentMappingRequest(
    Guid? OrgDepartmentId,
    bool? IsActive,
    bool UpdateOrgDepartmentId = false);

public sealed record ImportDepartmentsFromDirectoryRequest(bool CreateMissingDepartments = true);

public sealed record ImportDepartmentsFromDirectoryResultDto(
    int MappingsImported,
    int DepartmentsCreated,
    int DepartmentsLinked,
    int UnmappedCount);
