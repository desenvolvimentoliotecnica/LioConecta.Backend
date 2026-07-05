using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Workers.Jobs;

public sealed class GraphSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GraphSyncWorker> _logger;
    private readonly IAppSettingsProvider _settings;

    public GraphSyncWorker(
        IServiceProvider services,
        ILogger<GraphSyncWorker> logger,
        IAppSettingsProvider settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _settings.GetInt(AppSettingKeys.WorkersGraphSyncIntervalMinutes, 60);
        _logger.LogInformation("Graph sync worker started (interval: {Interval} min)", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncDocumentsAsync(stoppingToken);
                await SyncCalendarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Graph sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    private async Task SyncDocumentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var graph = scope.ServiceProvider.GetRequiredService<IGraphAdapter>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var documents = await graph.GetDocumentsAsync(category: null, cancellationToken);
        foreach (var doc in documents)
        {
            var existing = await db.Documents
                .FirstOrDefaultAsync(d => d.SharePointItemId == doc.ItemId, cancellationToken);

            if (existing is null)
            {
                existing = new DocumentMetadata
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
                db.Documents.Add(existing);
            }

            existing.Title = doc.Title;
            existing.Category = doc.Category;
            existing.SharePointUrl = doc.WebUrl;
            existing.SharePointItemId = doc.ItemId;
            existing.ModifiedAt = doc.ModifiedAt;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Graph document sync completed: {Count} items", documents.Count);
    }

    private async Task SyncCalendarAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var graph = scope.ServiceProvider.GetRequiredService<IGraphAdapter>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var referencePerson = await db.People
            .Where(p => p.IsActive)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (referencePerson is null)
        {
            _logger.LogWarning("Graph calendar sync skipped: no active people in database");
            return;
        }

        var from = DateTimeOffset.UtcNow.Date;
        var to = from.AddDays(30);
        var events = await graph.GetCalendarEventsAsync(referencePerson.Id, from, to, cancellationToken);

        foreach (var evt in events)
        {
            var existing = await db.CalendarEvents
                .FirstOrDefaultAsync(e => e.ExternalId == evt.ExternalId, cancellationToken);

            if (existing is null)
            {
                existing = new CalendarEvent
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
                db.CalendarEvents.Add(existing);
            }

            existing.Title = evt.Title;
            existing.StartAt = evt.StartAt;
            existing.EndAt = evt.EndAt;
            existing.Location = evt.Location;
            existing.Source = "Outlook";
            existing.ExternalId = evt.ExternalId;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Graph calendar sync completed: {Count} events", events.Count);
    }
}
