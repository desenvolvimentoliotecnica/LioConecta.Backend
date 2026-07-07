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
        if (context is not null)
        {
            await context.LogInfoAsync(
                "Graph calendar cache sync skipped: delegated Outlook calendar is served live via /api/v1/calendar.",
                cancellationToken);
        }

        await Task.CompletedTask;
    }
}
