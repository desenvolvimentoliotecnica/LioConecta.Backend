using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class TotvsTimesheetSyncWorker(
    IServiceProvider services,
    ILogger<TotvsTimesheetSyncWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersTotvsTimesheetSyncIntervalMinutes, 30);
        logger.LogInformation("TOTVS timesheet sync worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var timesheetSync = scope.ServiceProvider.GetRequiredService<ITimesheetSyncService>();

                await recorder.ExecuteAsync(
                    WorkerKeys.TotvsTimesheetSync,
                    "scheduled",
                    async (context, ct) => _ = await timesheetSync.SyncAllActivePeopleAsync(context, ct),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "TOTVS timesheet sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
