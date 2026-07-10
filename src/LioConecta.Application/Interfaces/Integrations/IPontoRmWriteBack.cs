namespace LioConecta.Application.Interfaces.Integrations;

public sealed record PontoRmWriteBackPunch(
    DateOnly Date,
    string? ClockIn,
    string? LunchOut,
    string? LunchIn,
    string? ClockOut);

public sealed record PontoRmWriteBackCommand(
    Guid RecordId,
    Guid PersonId,
    string Chapa,
    IReadOnlyList<PontoRmWriteBackPunch> Days);

public sealed record PontoRmWriteBackResult(
    bool Success,
    string Status,
    string? ExternalId,
    string Message);

public interface IPontoRmWriteBack
{
    Task<PontoRmWriteBackResult> SubmitAdjustmentAsync(
        PontoRmWriteBackCommand command,
        CancellationToken cancellationToken = default);
}
