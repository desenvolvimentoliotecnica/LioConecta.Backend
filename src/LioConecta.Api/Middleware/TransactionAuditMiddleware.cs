using LioConecta.Application.Common.Audit;

namespace LioConecta.Api.Middleware;

public sealed class TransactionAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuditContextAccessor contextAccessor)
    {
        var auditContext = contextAccessor.Current;
        if (auditContext is null)
        {
            await next(context);
            return;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Audit-Transaction-Id"] = auditContext.TransactionId.ToString();
            return Task.CompletedTask;
        });

        await next(context);
    }
}
