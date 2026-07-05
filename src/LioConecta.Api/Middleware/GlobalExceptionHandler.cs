using System.Diagnostics;
using LioConecta.Application.Common.Audit;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var traceId = Activity.Current?.TraceId.ToString();

        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path} correlationId={CorrelationId} traceId={TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            correlationId,
            traceId);

        var (statusCode, title, detail) = exception switch
        {
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, "Resource not found", ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", "Access to the resource was denied."),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Bad request", ex.Message),
            InvalidOperationException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred",
                "Não foi possível concluir a operação."),
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId.ToString(),
            },
        };

        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        httpContext.Response.Headers[AuditContext.CorrelationHeaderName] = correlationId.ToString();
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static Guid ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Items[AuditContext.HttpContextItemKey] is AuditContext auditContext)
        {
            return auditContext.CorrelationId;
        }

        var header = httpContext.Request.Headers[AuditContext.CorrelationHeaderName].FirstOrDefault();
        if (Guid.TryParse(header, out var parsed))
        {
            return parsed;
        }

        return Guid.NewGuid();
    }
}
