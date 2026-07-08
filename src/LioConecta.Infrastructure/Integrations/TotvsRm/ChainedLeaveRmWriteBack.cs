using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Totvs;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public sealed class ChainedLeaveRmWriteBack(
    IAppSettingsProvider settings,
    TotvsRmApiLeaveWriteBack apiWriteBack,
    QueuedLeaveRmWriteBack queuedWriteBack) : ILeaveRmWriteBack
{
    public Task<LeaveRmWriteBackResult> SubmitVacationRequestAsync(
        LeaveRmWriteBackCommand command,
        CancellationToken cancellationToken = default) =>
        settings.GetBool(AppSettingKeys.LeaveRmWriteBackEnabled, false)
            ? apiWriteBack.SubmitVacationRequestAsync(command, cancellationToken)
            : queuedWriteBack.SubmitVacationRequestAsync(command, cancellationToken);
}
