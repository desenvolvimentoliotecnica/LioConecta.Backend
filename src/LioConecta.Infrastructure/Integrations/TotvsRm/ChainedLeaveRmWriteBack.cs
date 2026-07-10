using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

/// <summary>
/// Roteia o write-back de férias conforme "leave.rm.writeback.mode" (Onda 1B — decisão de
/// escrita SQL direta, ver docs/spike-writeback-sql-rm.md):
/// off → fila local (QueuedLeaveRmWriteBack); dry_run/apply_rollbackable/apply → SQL direto no RM.
/// </summary>
public sealed class ChainedLeaveRmWriteBack(
    IAppSettingsProvider settings,
    TotvsRmSqlLeaveWriteBack sqlWriteBack,
    QueuedLeaveRmWriteBack queuedWriteBack) : ILeaveRmWriteBack
{
    public Task<LeaveRmWriteBackResult> SubmitVacationRequestAsync(
        LeaveRmWriteBackCommand command,
        CancellationToken cancellationToken = default)
    {
        var mode = RmWriteBackModes.ResolveLeaveMode(settings);
        return mode == RmWriteBackModes.Off
            ? queuedWriteBack.SubmitVacationRequestAsync(command, cancellationToken)
            : sqlWriteBack.SubmitVacationRequestAsync(command, cancellationToken);
    }
}
