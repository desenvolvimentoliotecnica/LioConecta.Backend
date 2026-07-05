using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class GraphSyncWorker(
    IServiceProvider services,
    ILogger<GraphSyncWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersGraphSyncIntervalMinutes, 60);
        logger.LogInformation("Graph sync worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var graphSync = scope.ServiceProvider.GetRequiredService<IGraphSyncService>();

                await recorder.ExecuteAsync(
                    WorkerKeys.GraphSync,
                    "scheduled",
                    async (context, ct) =>
                    {
                        await graphSync.SyncDocumentsAsync(context, ct);
                        await graphSync.SyncCalendarAsync(context, ct);
                    },
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Graph sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
