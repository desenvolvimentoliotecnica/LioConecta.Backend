using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Ldap;

namespace LioConecta.Infrastructure.Services;

public sealed class LdapConfigurationService(
    LdapSettingsResolver settingsResolver,
    LdapConnectionTester connectionTester) : ILdapConfigurationService
{
    public async Task<LdapConnectionTestResponse> TestConnectionAsync(
        TestLdapConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = settingsResolver.Resolve(
            request.Host,
            request.Port,
            request.UseSsl,
            request.BindDn,
            request.BindPassword,
            request.SearchBase);

        return await connectionTester.TestAsync(settings, cancellationToken);
    }
}
