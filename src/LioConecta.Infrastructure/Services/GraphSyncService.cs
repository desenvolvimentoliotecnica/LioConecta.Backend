using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class GraphSyncService(
    IGraphAdapter graphAdapter,
    AppDbContext db) : IGraphSyncService
{
    public async Task SyncDocumentsAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var documents = await graphAdapter.GetDocumentsAsync(category: null, cancellationToken);
        foreach (var doc in documents)
        {
            var existing = await db.Documents
                .FirstOrDefaultAsync(d => d.SharePointItemId == doc.ItemId, cancellationToken);

            if (existing is null)
            {
                existing = new DocumentMetadata
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.Documents.Add(existing);
            }

            existing.Title = doc.Title;
            existing.Category = doc.Category;
            existing.SharePointUrl = doc.WebUrl;
            existing.SharePointItemId = doc.ItemId;
            existing.ModifiedAt = doc.ModifiedAt;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (context is not null)
        {
            await context.LogInfoAsync($"Graph document sync completed: {documents.Count} items.", cancellationToken);
        }
    }

    public async Task SyncCalendarAsync(
        IWorkerRunContext? context,
        CancellationToken cancellationToken)
    {
        var referencePerson = await db.People
            .Where(p => p.IsActive)
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (referencePerson is null)
        {
            if (context is not null)
            {
                await context.LogWarningAsync("Graph calendar sync skipped: no active people in database.", cancellationToken);
            }

            return;
        }

        var from = DateTimeOffset.UtcNow.Date;
        var to = from.AddDays(30);
        var events = await graphAdapter.GetCalendarEventsAsync(referencePerson.Id, from, to, cancellationToken);

        foreach (var evt in events)
        {
            var existing = await db.CalendarEvents
                .FirstOrDefaultAsync(e => e.ExternalId == evt.ExternalId, cancellationToken);

            if (existing is null)
            {
                existing = new CalendarEvent
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.CalendarEvents.Add(existing);
            }

            existing.Title = evt.Title;
            existing.StartAt = evt.StartAt.ToUniversalTime();
            existing.EndAt = evt.EndAt.ToUniversalTime();
            existing.Location = evt.Location;
            existing.Source = "Outlook";
            existing.ExternalId = evt.ExternalId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        if (context is not null)
        {
            await context.LogInfoAsync($"Graph calendar sync completed: {events.Count} events.", cancellationToken);
        }
    }
}
