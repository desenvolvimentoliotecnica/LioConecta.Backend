using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class NewsScheduleWorker(
    IServiceProvider services,
    ILogger<NewsScheduleWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersNewsScheduleIntervalMinutes, 1);
        logger.LogInformation("News schedule worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();
                await recorder.ExecuteAsync(
                    WorkerKeys.NewsSchedule,
                    "scheduled",
                    async (context, ct) =>
                    {
                        var published = await feedService.PublishScheduledNewsAsync(ct);
                        await context.LogInfoAsync($"Published {published} scheduled news post(s).", ct);
                    },
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "News schedule processing failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
