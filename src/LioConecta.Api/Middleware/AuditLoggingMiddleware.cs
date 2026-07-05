using LioConecta.Application.Common.Audit;
using Serilog.Context;

namespace LioConecta.Api.Middleware;

public sealed class AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAuditContextAccessor contextAccessor)
    {
        var auditContext = contextAccessor.Current;

        if (auditContext is null)
        {
            await next(context);
            return;
        }

        using (LogContext.PushProperty("CorrelationId", auditContext.CorrelationId))
        using (LogContext.PushProperty("TransactionId", auditContext.TransactionId))
        using (LogContext.PushProperty("AuditPath", auditContext.Path))
        {
            await next(context);

            if (AuditMiddleware.IsMutableApiRequest(context.Request))
            {
                var durationMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - auditContext.StartedAt).TotalMilliseconds);
                logger.LogInformation(
                    "Audit HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms (CorrelationId={CorrelationId})",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    durationMs,
                    auditContext.CorrelationId);
            }
        }
    }
}
