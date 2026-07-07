using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Graph;

namespace LioConecta.Infrastructure.Services;

public sealed class CalendarConfigurationService(
    IAppSettingsProvider settingsProvider,
    IAppSettingRepository appSettingRepository,
    CalendarConnectionTester connectionTester) : ICalendarConfigurationService
{
    private const string SecretMask = "********";

    public async Task<CalendarConnectionTestResponse> TestConnectionAsync(
        TestCalendarConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var calendarEnabled = settingsProvider.GetBool(AppSettingKeys.CalendarEnabled, false);
        var tenantId = settingsProvider.GetString(AppSettingKeys.AzureAdTenantId);
        var clientId = settingsProvider.GetString(AppSettingKeys.AzureAdClientId);

        if (!calendarEnabled)
        {
            return new CalendarConnectionTestResponse(
                false,
                "Integração de calendário desabilitada.",
                "Ative «Calendário Outlook — habilitado» nesta seção e salve.",
                CalendarEnabled: false,
                TenantId: tenantId,
                ClientId: clientId);
        }

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
        {
            return new CalendarConnectionTestResponse(
                false,
                "Azure AD não configurado.",
                "Preencha tenant ID e client ID na seção Azure AD.",
                CalendarEnabled: true,
                TenantId: tenantId,
                ClientId: clientId);
        }

        var encryptionKey = ResolveTokenEncryptionKey(request.TokenEncryptionKey);
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return new CalendarConnectionTestResponse(
                false,
                "Chave de criptografia de tokens não configurada.",
                $"Informe '{AppSettingKeys.CalendarTokenEncryptionKey}' nesta seção Calendário Outlook (pode testar antes de salvar).",
                CalendarEnabled: true,
                TenantId: tenantId,
                ClientId: clientId);
        }

        var result = await connectionTester.TestAsync(cancellationToken);
        await PersistTestMetadataAsync(result, cancellationToken);

        return result with
        {
            CalendarEnabled = calendarEnabled,
            TenantId = tenantId,
            ClientId = clientId
        };
    }

    private async Task PersistTestMetadataAsync(
        CalendarConnectionTestResponse result,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        await UpsertValueAsync(AppSettingKeys.CalendarLastTestUtc, now, cancellationToken);
        await UpsertValueAsync(
            AppSettingKeys.CalendarLastTestMessage,
            result.Success ? result.Message : $"{result.Message} — {result.Detail}",
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

    private string ResolveTokenEncryptionKey(string? incoming)
    {
        if (!string.IsNullOrWhiteSpace(incoming)
            && !string.Equals(incoming.Trim(), SecretMask, StringComparison.Ordinal))
        {
            return incoming.Trim();
        }

        return settingsProvider.GetString(AppSettingKeys.CalendarTokenEncryptionKey);
    }
}
