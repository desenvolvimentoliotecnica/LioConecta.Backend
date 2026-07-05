using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class PollClosureWorker(
    IServiceProvider services,
    ILogger<PollClosureWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersPollClosureIntervalMinutes, 1);
        logger.LogInformation("Poll closure worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var pollClosureService = scope.ServiceProvider.GetRequiredService<IPollClosureService>();

                await recorder.ExecuteAsync(
                    WorkerKeys.PollClosure,
                    "scheduled",
                    async (context, ct) =>
                    {
                        await pollClosureService.ProcessClosedPollsAsync(ct);
                        await context.LogInfoAsync("Poll closure processing completed.", ct);
                    },
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Poll closure processing failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
