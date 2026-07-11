using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class ComunicadoScheduleWorker(
    IServiceProvider services,
    ILogger<ComunicadoScheduleWorker> logger,
    IAppSettingsProvider settings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = settings.GetInt(AppSettingKeys.WorkersComunicadoScheduleIntervalMinutes, 1);
        logger.LogInformation("Comunicado schedule worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var recorder = scope.ServiceProvider.GetRequiredService<IWorkerRunRecorder>();
                var comunicadoService = scope.ServiceProvider.GetRequiredService<IComunicadoService>();
                await recorder.ExecuteAsync(
                    WorkerKeys.ComunicadoSchedule,
                    "scheduled",
                    async (context, ct) =>
                    {
                        var published = await comunicadoService.PublishScheduledAsync(ct);
                        await context.LogInfoAsync($"Published {published} scheduled comunicado(s).", ct);
                    },
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Comunicado schedule processing failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
