using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPontoService
{
    Task<PontoResponseDto> GetTimesheetAsync(
        int? month,
        int? year,
        CancellationToken cancellationToken);

    Task<PontoPeriodSettingsDto> GetPeriodSettingsAsync(CancellationToken cancellationToken);
}

public interface ITimesheetSyncService
{
    Task<PontoResponseDto> SyncPersonAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken);

    Task<int> SyncAllActivePeopleAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken);
}

public interface IPayslipSyncService
{
    Task<PayslipSyncResultDto> SyncPersonAsync(
        Guid personId,
        CancellationToken cancellationToken);

    Task<bool> SyncIncomeStatementAsync(
        Guid personId,
        int year,
        CancellationToken cancellationToken);

    Task<int> SyncAllActivePeopleAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken);
}

public interface ITotvsEmployeeSyncService
{
    Task<int> SyncEmployeesAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken);
}

public interface IGraphSyncService
{
    Task SyncDocumentsAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken);

    Task SyncCalendarAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken);
}

public interface IGraphDirectorySyncService
{
    Task<GraphDirectorySyncResult> SyncDirectoryAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken);
}

public sealed record GraphDirectorySyncResult(
    int Created,
    int Updated,
    int Deactivated,
    int Fetched,
    int PhotosDownloaded,
    int PhotosMissing,
    int PhotosFailed,
    DateTimeOffset SyncedAtUtc);

public interface IWorkerRunContext
{
    Guid RunId { get; }

    Task LogInfoAsync(string message, CancellationToken cancellationToken);

    Task LogWarningAsync(string message, CancellationToken cancellationToken);

    Task LogErrorAsync(string message, CancellationToken cancellationToken);
}

public interface IWorkerRunRecorder
{
    Task<WorkerTriggerResultDto> ExecuteAsync(
        string workerKey,
        string triggerSource,
        Func<IWorkerRunContext, CancellationToken, Task> action,
        CancellationToken cancellationToken);
}

public interface IWorkerTriggerService
{
    Task<IReadOnlyList<WorkerDefinitionDto>> ListWorkersAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkerRunDto>> ListRunsAsync(
        string workerKey,
        int limit,
        CancellationToken cancellationToken);

    Task<WorkerRunDetailDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken);

    Task<WorkerTriggerResultDto> TriggerAsync(string workerKey, CancellationToken cancellationToken);
}

public interface IWorkersConnectivityService
{
    Task<WorkerConnectivityDto> CheckAsync(CancellationToken cancellationToken);
}
