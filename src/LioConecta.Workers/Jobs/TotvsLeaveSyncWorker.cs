using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class TotvsLeaveSyncWorker(
    IServiceProvider services,
    ILogger<TotvsLeaveSyncWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersTotvsLeaveSyncIntervalMinutes, 60);
        logger.LogInformation("TOTVS leave sync worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var leaveSync = scope.ServiceProvider.GetRequiredService<ILeaveSyncService>();

                await recorder.ExecuteAsync(
                    WorkerKeys.TotvsLeaveSync,
                    "scheduled",
                    async (context, ct) => _ = await leaveSync.SyncAllActivePeopleAsync(context, ct),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "TOTVS leave sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
