using LioConecta.Application.Common.Audit;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Api.Middleware;

public sealed class AuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IAuditContextAccessor contextAccessor,
        ICurrentUserService currentUserService)
    {
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

        if (context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                auditContext.ActorId = await currentUserService.GetPersonIdAsync(context.RequestAborted);
            }
            catch
            {
                // Best effort.
            }
        }

        contextAccessor.Set(auditContext);
        context.Items[AuditContext.HttpContextItemKey] = auditContext;
        context.Response.Headers[AuditContext.CorrelationHeaderName] = correlationId.ToString();

        if (IsMutableApiRequest(context.Request))
        {
            context.Request.EnableBuffering();
        }

        await next(context);
    }

    internal static bool IsMutableApiRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
        request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
}
