using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Api.Services;

public sealed class PollClosureHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<PollClosureHostedService> _logger;

    public PollClosureHostedService(IServiceProvider services, ILogger<PollClosureHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Poll closure hosted service started (interval: {Interval})", Interval);

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
