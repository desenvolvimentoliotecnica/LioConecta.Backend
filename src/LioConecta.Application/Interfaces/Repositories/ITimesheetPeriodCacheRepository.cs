using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Repositories;

public interface ITimesheetPeriodCacheRepository
{
    Task<TimesheetPeriodCacheDto?> GetAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        Guid personId,
        int year,
        int month,
        PontoSummaryDto summary,
        IReadOnlyList<PontoEntryDto> entries,
        DateTimeOffset syncedAtUtc,
        string source,
        CancellationToken cancellationToken);
}

public sealed record TimesheetPeriodCacheDto(
    Guid PersonId,
    int Year,
    int Month,
    PontoSummaryDto Summary,
    IReadOnlyList<PontoEntryDto> Entries,
    DateTimeOffset SyncedAtUtc,
    string Source);
