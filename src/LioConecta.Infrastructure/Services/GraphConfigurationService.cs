using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Graph;

namespace LioConecta.Infrastructure.Services;

public sealed class GraphConfigurationService(
    IAppSettingsProvider settingsProvider,
    GraphConnectionTester connectionTester) : IGraphConfigurationService
{
    private const string SecretMask = "********";

    public async Task<GraphConnectionTestResponse> TestConnectionAsync(
        TestGraphConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var credentials = ResolveCredentials(request);
        return await connectionTester.TestAsync(credentials, cancellationToken);
    }

    private GraphRuntimeCredentials ResolveCredentials(TestGraphConnectionRequest request)
    {
        var tenantId = FirstNonEmpty(
            request.TenantId,
            settingsProvider.GetString(AppSettingKeys.GraphTenantId));

        var clientId = FirstNonEmpty(
            request.ClientId,
            settingsProvider.GetString(AppSettingKeys.GraphClientId));

        var clientSecret = ResolveSecret(request.ClientSecret);

        return new GraphRuntimeCredentials(tenantId, clientId, clientSecret);
    }

    private string ResolveSecret(string? incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming)
            && !string.Equals(incoming.Trim(), SecretMask, StringComparison.Ordinal))
        {
            return incoming.Trim();
        }

        return settingsProvider.GetString(AppSettingKeys.GraphClientSecret);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
