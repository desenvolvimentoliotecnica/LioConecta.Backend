using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Glpi;

namespace LioConecta.Infrastructure.Services;

public sealed class GlpiConfigurationService(
    IAppSettingsProvider settingsProvider,
    GlpiConnectionTester connectionTester,
    GlpiCredentialsResolver credentialsResolver) : IGlpiConfigurationService
{
    public async Task<GlpiConnectionTestResponse> TestConnectionAsync(
        TestGlpiConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve(request.BaseUrl, request.AppToken, request.UserToken);
        return await connectionTester.TestAsync(credentials, cancellationToken);
    }
}
