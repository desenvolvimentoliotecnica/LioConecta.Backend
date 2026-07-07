using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class CalendarConnectionTester(IAppSettingsProvider settingsProvider)
{
    public Task<CalendarConnectionTestResponse> TestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tenantId = settingsProvider.GetString(AppSettingKeys.AzureAdTenantId);
        var clientId = settingsProvider.GetString(AppSettingKeys.AzureAdClientId);
        var scopesRaw = settingsProvider.GetString(AppSettingKeys.CalendarDelegatedScopes);

        if (string.IsNullOrWhiteSpace(scopesRaw))
        {
            return Task.FromResult(new CalendarConnectionTestResponse(
                false,
                "Scopes delegados não configurados.",
                "Configure calendar.delegated_scopes com Calendars.ReadWrite.",
                CalendarEnabled: true,
                TenantId: tenantId,
                ClientId: clientId));
        }

        if (!scopesRaw.Contains("Calendars.ReadWrite", StringComparison.OrdinalIgnoreCase)
            && !scopesRaw.Contains("Calendars.Read", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new CalendarConnectionTestResponse(
                false,
                "Scope de calendário ausente.",
                "Inclua Calendars.ReadWrite em calendar.delegated_scopes e conceda admin consent no Azure Portal.",
                CalendarEnabled: true,
                TenantId: tenantId,
                ClientId: clientId));
        }

        return Task.FromResult(new CalendarConnectionTestResponse(
            true,
            "Configuração de calendário válida.",
            "Azure AD e scopes delegados estão configurados. Usuários devem vincular a conta Microsoft em /calendario.",
            CalendarEnabled: true,
            TenantId: tenantId,
            ClientId: clientId));
    }
}
