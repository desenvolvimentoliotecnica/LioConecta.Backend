using LioConecta.Application.Common.Audit;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Api.Middleware;

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IAuditContextAccessor contextAccessor)
    {
        if (context.Items.ContainsKey(AuditContext.HttpContextItemKey))
        {
            await next(context);
            return;
        }

        var correlationHeader = context.Request.Headers[AuditContext.CorrelationHeaderName].FirstOrDefault();
        var correlationId = Guid.TryParse(correlationHeader, out var parsedCorrelation)
            ? parsedCorrelation
            : Guid.NewGuid();

        var auditContext = new AuditContext
        {
            CorrelationId = correlationId,
            TransactionId = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            HttpMethod = context.Request.Method,
            Path = context.Request.Path.Value,
        };

        context.Items[AuditContext.HttpContextItemKey] = auditContext;
        contextAccessor.Set(auditContext);
        context.Response.Headers[AuditContext.CorrelationHeaderName] = correlationId.ToString();

        await next(context);
    }
}
