using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class EmailDispatchWorker(
    IServiceProvider services,
    ILogger<EmailDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email dispatch worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = 30;

            try
            {
                using var scope = services.CreateScope();
                var configurationService = scope.ServiceProvider.GetRequiredService<IEmailConfigurationService>();
                var config = await configurationService.GetRuntimeConfigurationAsync(stoppingToken);
                intervalSeconds = Math.Clamp(config.DispatchIntervalSeconds, 5, 3600);

                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var dispatchService = scope.ServiceProvider.GetRequiredService<IEmailDispatchService>();

                await recorder.ExecuteAsync(
                    Application.Common.WorkerKeys.EmailDispatch,
                    "scheduled",
                    async (context, ct) =>
                    {
                        var result = await dispatchService.ProcessBatchAsync(ct);
                        await context.LogInfoAsync(
                            $"Email dispatch: processed={result.Processed}, sent={result.Sent}, failed={result.Failed}, skipped={result.Skipped}",
                            ct);
                    },
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Email dispatch processing failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}
