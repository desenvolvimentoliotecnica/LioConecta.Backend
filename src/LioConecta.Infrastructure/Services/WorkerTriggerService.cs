using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class WorkerTriggerService(
    AppDbContext db,
    IAppSettingsProvider settings,
    IWorkerRunRecorder workerRunRecorder,
    ITotvsEmployeeSyncService totvsEmployeeSyncService,
    IGraphSyncService graphSyncService,
    IGraphDirectorySyncService graphDirectorySyncService,
    IPollClosureService pollClosureService,
    ITimesheetSyncService timesheetSyncService,
    IPayslipSyncService payslipSyncService,
    IEmailDispatchService emailDispatchService) : IWorkerTriggerService
{
    public Task<IReadOnlyList<WorkerDefinitionDto>> ListWorkersAsync(CancellationToken cancellationToken)
    {
        var workers = WorkerRegistry.All
            .Select(worker =>
            {
                var configuredInterval = worker.IntervalSettingKey is null
                    ? worker.DefaultIntervalMinutes
                    : settings.GetInt(
                        worker.IntervalSettingKey,
                        worker.DefaultIntervalMinutes ?? 0);

                return worker with { DefaultIntervalMinutes = configuredInterval };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<WorkerDefinitionDto>>(workers);
    }

    public async Task<IReadOnlyList<WorkerRunDto>> ListRunsAsync(
        string workerKey,
        int limit,
        CancellationToken cancellationToken)
    {
        var cappedLimit = Math.Clamp(limit, 1, 100);
        var runs = await db.WorkerRuns
            .AsNoTracking()
            .Where(r => r.WorkerKey == workerKey)
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(cappedLimit)
            .Select(r => new WorkerRunDto(
                r.Id,
                r.WorkerKey,
                r.Status,
                r.TriggerSource,
                r.StartedAtUtc,
                r.FinishedAtUtc,
                r.ErrorMessage))
            .ToListAsync(cancellationToken);

        return runs;
    }

    public async Task<WorkerRunDetailDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await db.WorkerRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run is null)
        {
            return null;
        }

        var logs = await db.WorkerRunLogs
            .AsNoTracking()
            .Where(l => l.WorkerRunId == runId)
            .OrderBy(l => l.LoggedAtUtc)
            .Select(l => new WorkerRunLogDto(l.Id, l.LoggedAtUtc, l.Level, l.Message))
            .ToListAsync(cancellationToken);

        return new WorkerRunDetailDto(
            new WorkerRunDto(
                run.Id,
                run.WorkerKey,
                run.Status,
                run.TriggerSource,
                run.StartedAtUtc,
                run.FinishedAtUtc,
                run.ErrorMessage),
            logs);
    }

    public Task<WorkerTriggerResultDto> TriggerAsync(string workerKey, CancellationToken cancellationToken)
    {
        if (!WorkerRegistry.All.Any(w => w.Key == workerKey))
        {
            throw new ArgumentException($"Unknown worker key: {workerKey}", nameof(workerKey));
        }

        return workerKey switch
        {
            WorkerKeys.TotvsEmployeeSync => workerRunRecorder.ExecuteAsync(
                workerKey,
                "manual",
                async (context, ct) => _ = await totvsEmployeeSyncService.SyncEmployeesAsync(context, ct),
                cancellationToken),
            WorkerKeys.GraphSync => workerRunRecorder.ExecuteAsync(
                workerKey,
                "manual",
                async (context, ct) =>
                {
                    await graphSyncService.SyncDocumentsAsync(context, ct);
                    await graphSyncService.SyncCalendarAsync(context, ct);
                },
                cancellationToken),
            WorkerKeys.GraphDirectorySync => workerRunRecorder.ExecuteAsync(
                workerKey,
                "manual",
                async (context, ct) => _ = await graphDirectorySyncService.SyncDirectoryAsync(context, ct),
                cancellationToken),
            WorkerKeys.PollClosure => workerRunRecorder.ExecuteAsync(
                workerKey,
                "manual",
                async (context, ct) =>
                {
                    await pollClosureService.ProcessClosedPollsAsync(ct);
                    await context.LogInfoAsync("Poll closure processing completed.", ct);
                },
                cancellationToken),
            WorkerKeys.TotvsTimesheetSync => workerRunRecorder.ExecuteAsync(
                workerKey,
                "manual",
                async (context, ct) => _ = await timesheetSyncService.SyncAllActivePeopleAsync(context, ct),
                cancellationToken),
            WorkerKeys.TotvsPayslipSync => workerRunRecorder.ExecuteAsync(
                workerKey,
                "manual",
                async (context, ct) => _ = await payslipSyncService.SyncAllActivePeopleAsync(context, ct),
                cancellationToken),
            WorkerKeys.EmailDispatch => workerRunRecorder.ExecuteAsync(
                workerKey,
                "manual",
                async (context, ct) =>
                {
                    var result = await emailDispatchService.ProcessBatchAsync(ct);
                    await context.LogInfoAsync(
                        $"Email dispatch: processed={result.Processed}, sent={result.Sent}, failed={result.Failed}, skipped={result.Skipped}",
                        ct);
                },
                cancellationToken),
            _ => throw new ArgumentException($"Unknown worker key: {workerKey}", nameof(workerKey))
        };
    }
}
