using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Application.Interfaces.Services;

public interface IObservabilityIngestionService
{
    Task<TelemetryIngestResultDto> IngestEventsAsync(
        TelemetryEventsBatchDto batch,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<TelemetryIngestResultDto> IngestPageViewsAsync(
        TelemetryPageViewsBatchDto batch,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task RecordAccessAsync(
        AccessAuditEntry entry,
        string? remoteIp,
        string? userAgent,
        CancellationToken cancellationToken = default);
}

public interface IObservabilityRetentionService
{
    Task ExecuteRetentionAsync(CancellationToken cancellationToken = default);
}
