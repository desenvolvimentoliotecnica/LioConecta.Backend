using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Workers.Jobs;

public sealed class PollClosureWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<PollClosureWorker> _logger;

    public PollClosureWorker(IServiceProvider services, ILogger<PollClosureWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Poll closure worker started (interval: {Interval})", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var pollClosureService = scope.ServiceProvider.GetRequiredService<IPollClosureService>();
                await pollClosureService.ProcessClosedPollsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Poll closure processing failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
