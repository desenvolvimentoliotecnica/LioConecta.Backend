using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

/// <summary>
/// Roteia o write-back de ponto conforme "ponto.rm.writeback.mode" (Onda 1B):
/// off → fila local (QueuedPontoRmWriteBack); dry_run/apply_rollbackable/apply → SQL direto no RM.
/// </summary>
public sealed class ChainedPontoRmWriteBack(
    IAppSettingsProvider settings,
    TotvsRmSqlPontoWriteBack sqlWriteBack,
    QueuedPontoRmWriteBack queuedWriteBack) : IPontoRmWriteBack
{
    public Task<PontoRmWriteBackResult> SubmitAdjustmentAsync(
        PontoRmWriteBackCommand command,
        CancellationToken cancellationToken = default)
    {
        var mode = RmWriteBackModes.ResolvePontoMode(settings);
        return mode == RmWriteBackModes.Off
            ? queuedWriteBack.SubmitAdjustmentAsync(command, cancellationToken)
            : sqlWriteBack.SubmitAdjustmentAsync(command, cancellationToken);
    }
}
