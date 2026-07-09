using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;

namespace LioConecta.Infrastructure.Services;

internal static class UniLioAuthorization
{
    internal static readonly string[] AdminPersonaPermissions =
    [
        "unilio.courses.approve",
        "unilio.courses.publish",
        "unilio.compliance.manage",
        "unilio.paths.manage",
        "unilio.skills.manage",
    ];

    internal static readonly string[] InstructorPersonaPermissions =
    [
        "unilio.instructor.panel",
        "unilio.courses.author",
        "unilio.courses.edit.own",
    ];

    public static Task<bool> CanAccessAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.HasPermissionAsync("unilio.access", cancellationToken: cancellationToken);

    public static Task EnsureCanAccessAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("unilio.access", cancellationToken: cancellationToken);

    public static Task EnsureCanAuthorAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("unilio.courses.author", cancellationToken: cancellationToken);

    public static Task<bool> CanApproveCoursesAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.HasPermissionAsync("unilio.courses.approve", DataScope.Global, cancellationToken);

    public static Task EnsureCanApproveCoursesAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("unilio.courses.approve", DataScope.Global, cancellationToken);

    public static Task EnsureCanPublishCoursesAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("unilio.courses.publish", DataScope.Global, cancellationToken);

    public static Task<bool> CanUseInstructorPanelAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        HasAnyPermissionAsync(permissionService, InstructorPersonaPermissions, cancellationToken);

    public static Task EnsureInstructorPanelAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("unilio.instructor.panel", cancellationToken: cancellationToken);

    public static Task EnsureTeamViewAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("unilio.team.view", cancellationToken: cancellationToken);

    public static Task EnsureReportsViewAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("unilio.reports.view", cancellationToken: cancellationToken);

    public static async Task<bool> HasGlobalDataScopeAsync(
        IPermissionService permissionService,
        string permissionKey,
        CancellationToken cancellationToken) =>
        await permissionService.HasPermissionAsync(permissionKey, DataScope.Global, cancellationToken);

    public static async Task<bool> HasAnyPermissionAsync(
        IPermissionService permissionService,
        IReadOnlyList<string> permissionKeys,
        CancellationToken cancellationToken)
    {
        foreach (var key in permissionKeys)
        {
            if (await permissionService.HasPermissionAsync(key, cancellationToken: cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task<string> ResolvePersonaAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (await HasAnyPermissionAsync(permissionService, AdminPersonaPermissions, cancellationToken))
        {
            return "admin";
        }

        if (await HasAnyPermissionAsync(permissionService, InstructorPersonaPermissions, cancellationToken))
        {
            return "instructor";
        }

        if (await permissionService.HasPermissionAsync("unilio.team.view", cancellationToken: cancellationToken))
        {
            return "manager";
        }

        return "learner";
    }
}
