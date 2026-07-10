namespace LioConecta.Application.Interfaces.Services;

public sealed record AccessAuditEntry(
    string EventType,
    string EventName,
    Guid CorrelationId,
    Guid? UserId,
    string? UsernameSnapshot,
    Guid? SessionId,
    string? Resource,
    string? Action,
    string Result,
    string? ReasonCode,
    int? StatusCode,
    string? HttpMethod,
    string? Path,
    string? MetadataJson = null);

public interface IAccessAuditRecorder
{
    ValueTask RecordAsync(AccessAuditEntry entry, CancellationToken cancellationToken = default);
}
