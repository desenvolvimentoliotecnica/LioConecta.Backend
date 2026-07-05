using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class TimesheetPeriodCacheRepository(AppDbContext db) : ITimesheetPeriodCacheRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<TimesheetPeriodCacheDto?> GetAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var cache = await db.TimesheetPeriodCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.PersonId == personId && c.Year == year && c.Month == month,
                cancellationToken);

        if (cache is null)
        {
            return null;
        }

        var summary = JsonSerializer.Deserialize<PontoSummaryDto>(cache.SummaryJson, JsonOptions);
        var entries = JsonSerializer.Deserialize<List<PontoEntryDto>>(cache.EntriesJson, JsonOptions) ?? [];

        if (summary is null)
        {
            return null;
        }

        return new TimesheetPeriodCacheDto(
            cache.PersonId,
            cache.Year,
            cache.Month,
            summary,
            entries,
            cache.SyncedAtUtc,
            cache.Source);
    }

    public async Task UpsertAsync(
        Guid personId,
        int year,
        int month,
        PontoSummaryDto summary,
        IReadOnlyList<PontoEntryDto> entries,
        DateTimeOffset syncedAtUtc,
        string source,
        CancellationToken cancellationToken)
    {
        var cache = await db.TimesheetPeriodCaches
            .FirstOrDefaultAsync(
                c => c.PersonId == personId && c.Year == year && c.Month == month,
                cancellationToken);

        if (cache is null)
        {
            cache = new TimesheetPeriodCache
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Year = year,
                Month = month,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.TimesheetPeriodCaches.Add(cache);
        }

        cache.SummaryJson = JsonSerializer.Serialize(summary, JsonOptions);
        cache.EntriesJson = JsonSerializer.Serialize(entries, JsonOptions);
        cache.SyncedAtUtc = syncedAtUtc;
        cache.Source = source;
        cache.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}
