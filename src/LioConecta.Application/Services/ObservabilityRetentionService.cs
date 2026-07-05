using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Application.Services;

public sealed class ObservabilityRetentionService(
    IObservabilityRepository repository,
    IAppSettingsProvider settings,
    ILogger<ObservabilityRetentionService> logger) : IObservabilityRetentionService
{
    public async Task ExecuteRetentionAsync(CancellationToken cancellationToken = default)
    {
        if (!settings.GetBool(AppSettingKeys.ObservabilityRetentionEnabled, true))
        {
            logger.LogDebug("Observability retention disabled by configuration.");
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var observabilityDays = settings.GetInt(AppSettingKeys.ObservabilityRetentionObservabilityDays, 90);
        var pageViewDays = settings.GetInt(AppSettingKeys.ObservabilityRetentionPageViewDays, 180);
        var accessDays = settings.GetInt(AppSettingKeys.ObservabilityRetentionAccessEventDays, 365);

        var observabilityDeleted = await repository.PurgeObservabilityEventsAsync(
            now.AddDays(-observabilityDays),
            cancellationToken);

        var pageViewsDeleted = await repository.PurgePageViewsAsync(
            now.AddDays(-pageViewDays),
            cancellationToken);

        var accessDeleted = await repository.PurgeAccessEventsAsync(
            now.AddDays(-accessDays),
            cancellationToken);

        logger.LogInformation(
            "Observability retention completed. observability={ObservabilityDeleted}, pageViews={PageViewsDeleted}, access={AccessDeleted}",
            observabilityDeleted,
            pageViewsDeleted,
            accessDeleted);
    }
}
