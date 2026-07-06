using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ILdapConfigurationService
{
    Task<LdapConnectionTestResponse> TestConnectionAsync(
        TestLdapConnectionRequest request,
        CancellationToken cancellationToken = default);
}
