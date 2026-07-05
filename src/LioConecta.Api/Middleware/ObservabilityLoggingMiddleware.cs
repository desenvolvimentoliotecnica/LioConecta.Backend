using System.Diagnostics;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Common.Audit;
using LioConecta.Application.Common.Observability;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Api.Attributes;
using Serilog.Context;

namespace LioConecta.Api.Middleware;

public sealed class ObservabilityLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var auditContext = context.Items[AuditContext.HttpContextItemKey] as AuditContext;
        var correlationId = auditContext?.CorrelationId;
        var traceId = Activity.Current?.TraceId.ToString();
        var spanId = Activity.Current?.SpanId.ToString();
        var sessionHeader = context.Request.Headers[ObservabilityHeaders.SessionId].FirstOrDefault();
        Guid? sessionId = Guid.TryParse(sessionHeader, out var parsedSession) ? parsedSession : null;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("SpanId", spanId))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            await next(context);
        }
    }
}
