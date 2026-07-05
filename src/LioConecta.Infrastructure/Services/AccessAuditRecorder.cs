using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class AccessAuditRecorder(
    IObservabilityIngestionService ingestionService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AccessAuditRecorder> logger) : IAccessAuditRecorder
{
    public async ValueTask RecordAsync(AccessAuditEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Access audit {EventName} ({EventType}) result={Result} user={UserId} resource={Resource} correlation={CorrelationId}",
            entry.EventName,
            entry.EventType,
            entry.Result,
            entry.UserId,
            entry.Resource ?? entry.Path,
            entry.CorrelationId);

        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var remoteIp = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            await ingestionService.RecordAccessAsync(entry, remoteIp, userAgent, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to persist access audit event {EventName}", entry.EventName);
        }
    }
}
