using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class GraphDirectorySyncWorker(
    IServiceProvider services,
    ILogger<GraphDirectorySyncWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersGraphDirectorySyncIntervalMinutes, 60);
        logger.LogInformation(
            "Graph directory sync worker started (interval: {Interval} min)",
            intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var syncService = scope.ServiceProvider.GetRequiredService<IGraphDirectorySyncService>();

                await recorder.ExecuteAsync(
                    WorkerKeys.GraphDirectorySync,
                    "scheduled",
                    async (context, ct) => _ = await syncService.SyncDirectoryAsync(context, ct),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Graph directory sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
