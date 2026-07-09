using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace LioConecta.Api.Authorization;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionKey)
    {
        Policy = $"{AuthPolicies.PermissionPrefix}{permissionKey}";
    }
}

public sealed class PermissionRequirement(string permissionKey) : IAuthorizationRequirement
{
    public string PermissionKey { get; } = permissionKey;
}

public sealed class PermissionAuthorizationHandler(IPermissionService permissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (await permissionService.HasPermissionAsync(requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }
    }
}
