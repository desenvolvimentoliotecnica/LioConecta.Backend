using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ILeaveSyncService
{
    Task<LeaveSyncResultDto> SyncPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<int> SyncAllActivePeopleAsync(IWorkerRunContext? context, CancellationToken cancellationToken);
}

public sealed record LeaveSyncResultDto(
    int SyncedRecords,
    string AvailabilityStatus,
    string? DataSource,
    DateTimeOffset? SyncedAt);
