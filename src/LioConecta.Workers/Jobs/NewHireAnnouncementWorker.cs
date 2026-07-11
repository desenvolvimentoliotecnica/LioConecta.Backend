using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class NewHireAnnouncementWorker(IServiceProvider services, ILogger<NewHireAnnouncementWorker> logger, IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersNewHireAnnouncementIntervalMinutes, 60);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var service = scope.ServiceProvider.GetRequiredService<INewHireAnnouncementService>();
                await recorder.ExecuteAsync(WorkerKeys.NewHireAnnouncement, "scheduled", async (context, ct) =>
                { var announced = await service.AnnounceRecentHiresAsync(ct); await context.LogInfoAsync($"Announced {announced} new hire(s).", ct); }, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "New hire announcement processing failed"); }
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
