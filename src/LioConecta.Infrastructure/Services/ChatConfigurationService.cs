using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Graph;

namespace LioConecta.Infrastructure.Services;

public sealed class ChatConfigurationService(
    IAppSettingsProvider settingsProvider,
    IAppSettingRepository appSettingRepository,
    TeamsChatConnectionTester connectionTester) : IChatConfigurationService
{
    private const string SecretMask = "********";

    public async Task<ChatConnectionTestResponse> TestConnectionAsync(
        TestChatConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var chatEnabled = settingsProvider.GetBool(AppSettingKeys.ChatTeamsEnabled, false);
        var authMode = settingsProvider.GetString(AppSettingKeys.ChatTeamsAuthMode, "delegated");

        var azureAdConfigured =
            !string.IsNullOrWhiteSpace(settingsProvider.GetString(AppSettingKeys.AzureAdTenantId))
            && !string.IsNullOrWhiteSpace(settingsProvider.GetString(AppSettingKeys.AzureAdClientId));

        var encryptionKeyConfigured =
            !string.IsNullOrWhiteSpace(settingsProvider.GetString(AppSettingKeys.ChatTeamsTokenEncryptionKey));

        var graphCredentials = ResolveGraphCredentials(request);

        var result = await connectionTester.TestAsync(
            chatEnabled,
            authMode,
            azureAdConfigured,
            encryptionKeyConfigured,
            graphCredentials,
            cancellationToken);

        await PersistTestMetadataAsync(result, cancellationToken);
        return result;
    }

    private GraphRuntimeCredentials ResolveGraphCredentials(TestChatConnectionRequest request)
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

    private async Task PersistTestMetadataAsync(
        ChatConnectionTestResponse result,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        await UpsertValueAsync(AppSettingKeys.ChatTeamsLastTestUtc, now, cancellationToken);
        await UpsertValueAsync(
            AppSettingKeys.ChatTeamsLastTestMessage,
            result.Success ? result.Message : $"{result.Message} {result.Detail}".Trim(),
            cancellationToken);
    }

    private async Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        var existing = await appSettingRepository.GetByKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            return;
        }

        existing.Value = value;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await appSettingRepository.UpsertAsync(existing, cancellationToken);
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
