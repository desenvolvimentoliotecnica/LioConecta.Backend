using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public record EffectivePermissionDto(string Key, DataScope Scope);

public record PermissionCatalogItemDto(
    string Key,
    string Module,
    string Resource,
    string Action,
    string Label,
    string Description,
    BusinessArea BusinessArea,
    IReadOnlyList<DataScope> AllowedScopes,
    string? MenuPath);

public record RoleDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    BusinessArea? BusinessArea,
    bool IsSystem,
    bool IsKeyUserTemplate,
    bool IsActive,
    int PermissionCount);

public record RolePermissionDto(string PermissionKey, DataScope DataScope);

public record RoleDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    BusinessArea? BusinessArea,
    bool IsSystem,
    bool IsKeyUserTemplate,
    bool IsActive,
    IReadOnlyList<RolePermissionDto> Permissions);

public record UpsertRoleRequest(string Name, string? Description, BusinessArea? BusinessArea);

public record UpdateRolePermissionsRequest(IReadOnlyList<RolePermissionDto> Permissions);

public record SubjectRoleAssignmentDto(
    Guid Id,
    RbacSubjectType SubjectType,
    Guid SubjectId,
    string SubjectLabel,
    Guid RoleId,
    string RoleName,
    DateTimeOffset AssignedAt);

public record UpdateSubjectAssignmentsRequest(
    RbacSubjectType SubjectType,
    Guid SubjectId,
    IReadOnlyList<Guid> RoleIds);

public record BulkUpdateSubjectAssignmentsRequest(
    IReadOnlyList<UpdateSubjectAssignmentsRequest> Items);

public record RbacSubjectSearchResultDto(
    RbacSubjectType SubjectType,
    Guid SubjectId,
    string Label,
    string? Subtitle);

public record TestUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    BusinessArea BusinessArea,
    Guid? OptionalPersonId,
    bool IsActive,
    DateTimeOffset? ExpiresAt,
    string? Notes,
    IReadOnlyList<string> RoleNames);

public record CreateTestUserRequest(
    string Email,
    string Password,
    string DisplayName,
    BusinessArea BusinessArea,
    Guid? OptionalPersonId,
    DateTimeOffset? ExpiresAt,
    string? Notes,
    Guid? TemplateRoleId);

public record UpdateTestUserRequest(
    string DisplayName,
    BusinessArea BusinessArea,
    Guid? OptionalPersonId,
    bool IsActive,
    DateTimeOffset? ExpiresAt,
    string? Notes);

public record ResetTestUserPasswordRequest(string Password);

public record RbacBootstrapDto(
    IReadOnlyList<EffectivePermissionDto> Permissions,
    IReadOnlyDictionary<string, string> Menus,
    string? SubjectType,
    bool IsTestUser,
    BusinessArea? BusinessArea);

public record RbacAuthContext(
    RbacSubjectType SubjectType,
    Guid SubjectId,
    Guid? PersonId,
    string Email,
    string DisplayName,
    string SecurityStamp,
    bool IsTestUser);
