using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Api.Services;

public sealed class ObservabilityRetentionHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceProvider _services;
    private readonly ILogger<ObservabilityRetentionHostedService> _logger;

    public ObservabilityRetentionHostedService(
        IServiceProvider services,
        ILogger<ObservabilityRetentionHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Observability retention hosted service started (interval: {Interval})",
            Interval);

        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var retentionService = scope.ServiceProvider.GetRequiredService<IObservabilityRetentionService>();
                await retentionService.ExecuteRetentionAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Observability retention job failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
