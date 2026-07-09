using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/rbac")]
[Authorize]
public sealed class AdminRbacController(
    IRbacAdminService rbacAdminService,
    IPermissionService permissionService) : ControllerBase
{
    [HttpGet("permissions")]
    [RequirePermission("rbac.roles.manage")]
    public async Task<ActionResult<IReadOnlyList<PermissionCatalogItemDto>>> GetPermissions(CancellationToken cancellationToken)
        => Ok(await rbacAdminService.GetPermissionsAsync(cancellationToken));

    [HttpGet("roles")]
    [RequirePermission("rbac.roles.manage")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
        => Ok(await rbacAdminService.GetRolesAsync(cancellationToken));

    [HttpGet("roles/{id:guid}")]
    [RequirePermission("rbac.roles.manage")]
    public async Task<ActionResult<RoleDetailDto>> GetRole(Guid id, CancellationToken cancellationToken)
        => Ok(await rbacAdminService.GetRoleAsync(id, cancellationToken));

    [HttpPost("roles")]
    [RequirePermission("rbac.roles.manage")]
    public async Task<ActionResult<RoleDetailDto>> CreateRole([FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
        => Ok(await rbacAdminService.CreateRoleAsync(request, cancellationToken));

    [HttpPut("roles/{id:guid}")]
    [RequirePermission("rbac.roles.manage")]
    public async Task<ActionResult<RoleDetailDto>> UpdateRole(Guid id, [FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
        => Ok(await rbacAdminService.UpdateRoleAsync(id, request, cancellationToken));

    [HttpDelete("roles/{id:guid}")]
    [RequirePermission("rbac.roles.manage")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        await rbacAdminService.DeleteRoleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("roles/{id:guid}/permissions")]
    [RequirePermission("rbac.roles.manage")]
    public async Task<ActionResult<RoleDetailDto>> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
        => Ok(await rbacAdminService.UpdateRolePermissionsAsync(id, request, cancellationToken));

    [HttpGet("assignments")]
    [RequirePermission("rbac.assignments.manage")]
    public async Task<ActionResult<IReadOnlyList<SubjectRoleAssignmentDto>>> GetAssignments(
        [FromQuery] string? subjectType,
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        Domain.Enums.RbacSubjectType? type = null;
        if (!string.IsNullOrWhiteSpace(subjectType) && Enum.TryParse<Domain.Enums.RbacSubjectType>(subjectType, true, out var parsed))
        {
            type = parsed;
        }

        return Ok(await rbacAdminService.GetAssignmentsAsync(type, query, cancellationToken));
    }

    [HttpPut("assignments")]
    [RequirePermission("rbac.assignments.manage")]
    public async Task<IActionResult> UpdateAssignments([FromBody] UpdateSubjectAssignmentsRequest request, CancellationToken cancellationToken)
    {
        await rbacAdminService.UpdateAssignmentsAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPut("assignments/bulk")]
    [RequirePermission("rbac.assignments.manage")]
    public async Task<IActionResult> BulkUpdateAssignments([FromBody] BulkUpdateSubjectAssignmentsRequest request, CancellationToken cancellationToken)
    {
        await rbacAdminService.BulkUpdateAssignmentsAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("subjects/search")]
    [RequirePermission("rbac.assignments.manage")]
    public async Task<ActionResult<IReadOnlyList<RbacSubjectSearchResultDto>>> SearchSubjects(
        [FromQuery] string subjectType,
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Domain.Enums.RbacSubjectType>(subjectType, true, out var parsed))
        {
            return BadRequest("subjectType inválido.");
        }

        return Ok(await rbacAdminService.SearchSubjectsAsync(parsed, q ?? string.Empty, limit, cancellationToken));
    }

    [HttpGet("test-users")]
    [RequirePermission("rbac.test_users.manage")]
    public async Task<ActionResult<IReadOnlyList<TestUserDto>>> GetTestUsers(CancellationToken cancellationToken)
        => Ok(await rbacAdminService.GetTestUsersAsync(cancellationToken));

    [HttpPost("test-users")]
    [RequirePermission("rbac.test_users.manage")]
    public async Task<ActionResult<TestUserDto>> CreateTestUser([FromBody] CreateTestUserRequest request, CancellationToken cancellationToken)
        => Ok(await rbacAdminService.CreateTestUserAsync(request, cancellationToken));

    [HttpPut("test-users/{id:guid}")]
    [RequirePermission("rbac.test_users.manage")]
    public async Task<ActionResult<TestUserDto>> UpdateTestUser(Guid id, [FromBody] UpdateTestUserRequest request, CancellationToken cancellationToken)
        => Ok(await rbacAdminService.UpdateTestUserAsync(id, request, cancellationToken));

    [HttpDelete("test-users/{id:guid}")]
    [RequirePermission("rbac.test_users.manage")]
    public async Task<IActionResult> DeleteTestUser(Guid id, CancellationToken cancellationToken)
    {
        await rbacAdminService.DeleteTestUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("test-users/{id:guid}/reset-password")]
    [RequirePermission("rbac.test_users.manage")]
    public async Task<IActionResult> ResetTestUserPassword(Guid id, [FromBody] ResetTestUserPasswordRequest request, CancellationToken cancellationToken)
    {
        await rbacAdminService.ResetTestUserPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("bootstrap")]
    public async Task<ActionResult<RbacBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
        => Ok(await permissionService.GetBootstrapAsync(cancellationToken));
}
