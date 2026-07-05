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
        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad request"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
