using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class TotvsPayslipSyncWorker(
    IServiceProvider services,
    ILogger<TotvsPayslipSyncWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersTotvsPayslipSyncIntervalMinutes, 30);
        logger.LogInformation("TOTVS payslip sync worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var payslipSync = scope.ServiceProvider.GetRequiredService<IPayslipSyncService>();

                await recorder.ExecuteAsync(
                    WorkerKeys.TotvsPayslipSync,
                    "scheduled",
                    async (context, ct) => _ = await payslipSync.SyncAllActivePeopleAsync(context, ct),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "TOTVS payslip sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
