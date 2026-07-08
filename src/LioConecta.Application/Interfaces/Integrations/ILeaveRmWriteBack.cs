namespace LioConecta.Application.Interfaces.Integrations;

public sealed record LeaveRmWriteBackCommand(
    Guid RecordId,
    Guid PersonId,
    string Chapa,
    DateOnly StartDate,
    DateOnly EndDate,
    int Days,
    string? Notes);

public sealed record LeaveRmWriteBackResult(
    bool Success,
    string Status,
    string? ExternalId,
    string Message);

public interface ILeaveRmWriteBack
{
    Task<LeaveRmWriteBackResult> SubmitVacationRequestAsync(
        LeaveRmWriteBackCommand command,
        CancellationToken cancellationToken = default);
}
