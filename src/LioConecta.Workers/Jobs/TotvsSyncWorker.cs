using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class TotvsSyncWorker(
    IServiceProvider services,
    ILogger<TotvsSyncWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersTotvsSyncIntervalMinutes, 30);
        logger.LogInformation("TOTVS employee sync worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var syncService = scope.ServiceProvider.GetRequiredService<ITotvsEmployeeSyncService>();

                await recorder.ExecuteAsync(
                    WorkerKeys.TotvsEmployeeSync,
                    "scheduled",
                    async (context, ct) => _ = await syncService.SyncEmployeesAsync(context, ct),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "TOTVS employee sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
