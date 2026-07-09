using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;

namespace LioConecta.Infrastructure.Services;

internal static class BenefitManageAuthorization
{
    internal static readonly IReadOnlyList<string> Categories =
        ["saude", "alimentacao", "mobilidade", "qualidade", "familia"];

    internal static readonly IReadOnlyList<string> Statuses =
        ["obrigatorio", "opcional", "flexivel"];

    public static Task<bool> CanManageAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.HasPermissionAsync("benefits.manage", DataScope.Global, cancellationToken);

    public static Task EnsureCanManageAsync(
        IPermissionService permissionService,
        CancellationToken cancellationToken) =>
        permissionService.EnsurePermissionAsync("benefits.manage", DataScope.Global, cancellationToken);
}
