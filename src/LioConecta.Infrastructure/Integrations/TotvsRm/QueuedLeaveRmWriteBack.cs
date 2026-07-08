using LioConecta.Application.Interfaces.Integrations;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

/// <summary>
/// Enfileira solicitações até a API Labore TOTVS RM estar disponível.
/// </summary>
public sealed class QueuedLeaveRmWriteBack(ILogger<QueuedLeaveRmWriteBack> logger) : ILeaveRmWriteBack
{
    public Task<LeaveRmWriteBackResult> SubmitVacationRequestAsync(
        LeaveRmWriteBackCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Férias {RecordId} enfileirada para write-back RM (CHAPA {Chapa}, {Start}–{End}).",
            command.RecordId,
            command.Chapa,
            command.StartDate,
            command.EndDate);

        return Task.FromResult(new LeaveRmWriteBackResult(
            false,
            "pending_rm_sync",
            null,
            "Solicitação registrada no portal. O envio ao RM Labore será concluído quando a integração de API estiver ativa."));
    }
}
