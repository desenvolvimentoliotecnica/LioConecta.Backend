using LioConecta.Application.Common.Audit;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Api.Middleware;

public sealed class AuditTrailMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IAuditContextAccessor contextAccessor,
        IAuditService auditService)
    {
        await next(context);

        var auditContext = contextAccessor.Current;
        if (auditContext is null || !AuditMiddleware.IsMutableApiRequest(context.Request))
        {
            return;
        }

        var durationMs = (int)Math.Max(0, (DateTimeOffset.UtcNow - auditContext.StartedAt).TotalMilliseconds);
        var requestBody = await ReadRequestBodyAsync(context.Request);

        auditService.RecordHttp(
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Response.StatusCode,
            durationMs,
            auditContext.ActorId,
            requestBody);

        await auditService.FlushAsync(context.RequestAborted);
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek || request.ContentLength is 0)
        {
            return null;
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return string.IsNullOrWhiteSpace(body) ? null : body;
    }
}
