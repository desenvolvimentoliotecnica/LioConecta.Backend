using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IRbacAdminService
{
    Task<IReadOnlyList<PermissionCatalogItemDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleDetailDto> GetRoleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDetailDto> CreateRoleAsync(UpsertRoleRequest request, CancellationToken cancellationToken = default);

    Task<RoleDetailDto> UpdateRoleAsync(Guid id, UpsertRoleRequest request, CancellationToken cancellationToken = default);

    Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDetailDto> UpdateRolePermissionsAsync(Guid id, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectRoleAssignmentDto>> GetAssignmentsAsync(RbacSubjectType? subjectType, string? query, CancellationToken cancellationToken = default);

    Task UpdateAssignmentsAsync(UpdateSubjectAssignmentsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestUserDto>> GetTestUsersAsync(CancellationToken cancellationToken = default);

    Task<TestUserDto> CreateTestUserAsync(CreateTestUserRequest request, CancellationToken cancellationToken = default);

    Task<TestUserDto> UpdateTestUserAsync(Guid id, UpdateTestUserRequest request, CancellationToken cancellationToken = default);

    Task DeleteTestUserAsync(Guid id, CancellationToken cancellationToken = default);

    Task ResetTestUserPasswordAsync(Guid id, ResetTestUserPasswordRequest request, CancellationToken cancellationToken = default);
}
