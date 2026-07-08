using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Services;

namespace LioConecta.Workers.Jobs;

public sealed class LeaveWriteBackWorker(
    IServiceProvider services,
    ILogger<LeaveWriteBackWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Leave write-back worker started (interval: 15 min)");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (settings.GetBool(AppSettingKeys.LeaveRmWriteBackEnabled, false))
            {
                try
                {
                    using var scope = services.CreateScope();
                    var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                    var writeBack = scope.ServiceProvider.GetRequiredService<LeaveWriteBackService>();

                    await recorder.ExecuteAsync(
                        WorkerKeys.LeaveWriteBack,
                        "scheduled",
                        async (context, ct) =>
                        {
                            var processed = await writeBack.ProcessPendingAsync(ct);
                            await context.LogInfoAsync($"Processados {processed} write-back(s) de férias.", ct);
                        },
                        stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Leave write-back worker failed");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
