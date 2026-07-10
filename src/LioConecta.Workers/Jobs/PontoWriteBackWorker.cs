using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Services;

namespace LioConecta.Workers.Jobs;

public sealed class PontoWriteBackWorker(
    IServiceProvider services,
    ILogger<PontoWriteBackWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ponto write-back worker started (interval: 15 min)");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (RmWriteBackModes.ResolvePontoMode(settings) != RmWriteBackModes.Off)
            {
                try
                {
                    using var scope = services.CreateScope();
                    var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                    var writeBack = scope.ServiceProvider.GetRequiredService<PontoWriteBackService>();

                    await recorder.ExecuteAsync(
                        WorkerKeys.PontoWriteBack,
                        "scheduled",
                        async (context, ct) =>
                        {
                            var processed = await writeBack.ProcessPendingAsync(ct);
                            await context.LogInfoAsync($"Processados {processed} write-back(s) de ponto.", ct);
                        },
                        stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Ponto write-back worker failed");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
