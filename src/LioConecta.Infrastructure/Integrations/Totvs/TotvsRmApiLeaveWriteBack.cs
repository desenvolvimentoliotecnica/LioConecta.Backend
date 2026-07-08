using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Totvs;

/// <summary>
/// Write-back via REST middleware TOTVS quando configurado (Totvs.BaseUrl + ApiKey).
/// </summary>
public sealed class TotvsRmApiLeaveWriteBack(
    ITotvsAdapter totvsAdapter,
    IAppSettingsProvider settings,
    ILogger<TotvsRmApiLeaveWriteBack> logger) : ILeaveRmWriteBack
{
    public async Task<LeaveRmWriteBackResult> SubmitVacationRequestAsync(
        LeaveRmWriteBackCommand command,
        CancellationToken cancellationToken = default)
    {
        var enabled = settings.GetBool(AppSettingKeys.LeaveRmWriteBackEnabled, false);
        var baseUrl = settings.GetString(AppSettingKeys.TotvsBaseUrl);

        if (!enabled || string.IsNullOrWhiteSpace(baseUrl))
        {
            return new LeaveRmWriteBackResult(
                false,
                "pending_rm_sync",
                null,
                "Write-back RM desabilitado. Solicitação permanece na fila do portal.");
        }

        try
        {
            var externalId = await totvsAdapter.SubmitVacationRequestAsync(
                command.PersonId,
                command.StartDate,
                command.EndDate,
                cancellationToken);

            return new LeaveRmWriteBackResult(
                true,
                "synced",
                string.IsNullOrWhiteSpace(externalId) ? command.RecordId.ToString("N") : externalId,
                "Solicitação enviada ao RM com sucesso.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Falha no write-back RM para férias {RecordId}.",
                command.RecordId);

            return new LeaveRmWriteBackResult(
                false,
                "failed",
                null,
                "Não foi possível registrar no RM agora. A solicitação permanece pendente de sincronização.");
        }
    }
}
