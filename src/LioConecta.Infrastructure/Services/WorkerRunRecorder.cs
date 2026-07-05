using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class WorkerRunRecorder(AppDbContext db) : IWorkerRunRecorder
{
    public async Task<WorkerTriggerResultDto> ExecuteAsync(
        string workerKey,
        string triggerSource,
        Func<IWorkerRunContext, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var run = new WorkerRun
        {
            Id = Guid.NewGuid(),
            WorkerKey = workerKey,
            Status = "running",
            TriggerSource = triggerSource,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.WorkerRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var context = new WorkerRunContext(db, run.Id);

        try
        {
            await action(context, cancellationToken);
            run.Status = "succeeded";
            run.FinishedAtUtc = DateTimeOffset.UtcNow;
            run.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new WorkerTriggerResultDto(run.Id, workerKey, run.Status, null);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();

            run.Status = "failed";
            run.FinishedAtUtc = DateTimeOffset.UtcNow;
            run.ErrorMessage = ex.Message;
            run.UpdatedAt = DateTimeOffset.UtcNow;

            db.WorkerRuns.Update(run);
            db.WorkerRunLogs.Add(new WorkerRunLog
            {
                Id = Guid.NewGuid(),
                WorkerRunId = run.Id,
                LoggedAtUtc = DateTimeOffset.UtcNow,
                Level = "error",
                Message = ex.Message
            });

            await db.SaveChangesAsync(cancellationToken);

            return new WorkerTriggerResultDto(run.Id, workerKey, run.Status, ex.Message);
        }
    }

    private sealed class WorkerRunContext(AppDbContext db, Guid runId) : IWorkerRunContext
    {
        public Guid RunId => runId;

        public Task LogInfoAsync(string message, CancellationToken cancellationToken) =>
            AddLogAsync("info", message, cancellationToken);

        public Task LogWarningAsync(string message, CancellationToken cancellationToken) =>
            AddLogAsync("warning", message, cancellationToken);

        public Task LogErrorAsync(string message, CancellationToken cancellationToken) =>
            AddLogAsync("error", message, cancellationToken);

        private async Task AddLogAsync(string level, string message, CancellationToken cancellationToken)
        {
            db.WorkerRunLogs.Add(new WorkerRunLog
            {
                Id = Guid.NewGuid(),
                WorkerRunId = runId,
                LoggedAtUtc = DateTimeOffset.UtcNow,
                Level = level,
                Message = message
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
