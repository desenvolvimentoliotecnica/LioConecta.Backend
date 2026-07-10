using LioConecta.Application.Interfaces.Integrations;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

/// <summary>
/// Enfileira ajustes de ponto até o write-back RM ser habilitado (mode != off).
/// </summary>
public sealed class QueuedPontoRmWriteBack(ILogger<QueuedPontoRmWriteBack> logger) : IPontoRmWriteBack
{
    public Task<PontoRmWriteBackResult> SubmitAdjustmentAsync(
        PontoRmWriteBackCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Ajuste de ponto {RecordId} enfileirado para write-back RM (CHAPA {Chapa}, {DayCount} dia(s)).",
            command.RecordId,
            command.Chapa,
            command.Days.Count);

        return Task.FromResult(new PontoRmWriteBackResult(
            false,
            "pending_rm_sync",
            null,
            "Ajuste registrado no portal. O envio ao RM será concluído quando o write-back estiver ativo."));
    }
}
