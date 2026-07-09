using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IPermissionService
{
    Task<RbacAuthContext?> GetAuthContextAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EffectivePermissionDto>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(string permissionKey, DataScope? requiredScope = null, CancellationToken cancellationToken = default);

    Task EnsurePermissionAsync(string permissionKey, DataScope? requiredScope = null, CancellationToken cancellationToken = default);

    Task<RbacBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);
}
