using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Common.Audit;
using LioConecta.Application.Common.Observability;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Api.Attributes;

namespace LioConecta.Api.Middleware;

public sealed class AccessAuditMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task InvokeAsync(
        HttpContext context,
        IAppSettingsProvider settings,
        IAccessAuditRecorder recorder,
        ICurrentUserService currentUserService)
    {
        if (!settings.GetBool(AppSettingKeys.ObservabilityAccessAuditEnabled, true))
        {
            await next(context);
            return;
        }

        var shouldAudit = ShouldAuditRequest(context, settings, out var eventName, out var resource, out var action);
        if (!shouldAudit)
        {
            await next(context);
            return;
        }

        await next(context);

        await RecordAccessAsync(
            context,
            recorder,
            currentUserService,
            eventName,
            resource,
            action,
            AccessEventResults.Success);
    }

    internal static bool ShouldAuditRequest(
        HttpContext context,
        IAppSettingsProvider settings,
        out string eventName,
        out string? resource,
        out string? action)
    {
        eventName = ObservabilityEventNames.Resource.Viewed;
        resource = null;
        action = null;

        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<AccessAuditedAttribute>();
        if (attribute is not null)
        {
            eventName = attribute.EventName;
            resource = attribute.Resource ?? context.Request.Path.Value;
            action = attribute.Action;
            return true;
        }

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var patterns = LoadRoutePatterns(settings);
        var path = context.Request.Path.Value ?? string.Empty;
        foreach (var pattern in patterns)
        {
            if (AccessAuditRouteMatcher.Matches(context.Request.Method, path, pattern))
            {
                eventName = pattern.EventName;
                resource = path;
                return true;
            }
        }

        return false;
    }

    internal static IReadOnlyList<AccessAuditRoutePattern> LoadRoutePatterns(IAppSettingsProvider settings)
    {
        var json = settings.GetString(AppSettingKeys.ObservabilityAccessAuditRoutePatterns);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<AccessAuditRoutePattern>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    internal static async Task RecordAccessAsync(
        HttpContext context,
        IAccessAuditRecorder recorder,
        ICurrentUserService currentUserService,
        string eventName,
        string? resource,
        string? action,
        string result,
        string? reasonCode = null)
    {
        var auditContext = context.Items[AuditContext.HttpContextItemKey] as AuditContext;
        var correlationId = auditContext?.CorrelationId ??
            (Guid.TryParse(context.Request.Headers[AuditContext.CorrelationHeaderName].FirstOrDefault(), out var parsed)
                ? parsed
                : Guid.NewGuid());

        Guid? userId = auditContext?.ActorId;
        if (userId is null && context.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                userId = await currentUserService.GetPersonIdAsync(context.RequestAborted);
            }
            catch
            {
                // Best effort.
            }
        }

        var sessionHeader = context.Request.Headers[ObservabilityHeaders.SessionId].FirstOrDefault();
        Guid? sessionId = Guid.TryParse(sessionHeader, out var parsedSession) ? parsedSession : null;

        var username = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("preferred_username")?.Value ??
              context.User.Identity?.Name
            : null;

        await recorder.RecordAsync(new AccessAuditEntry(
            EventType: AccessEventTypes.ResourceAccess,
            EventName: eventName,
            CorrelationId: correlationId,
            UserId: userId,
            UsernameSnapshot: username,
            SessionId: sessionId,
            Resource: resource ?? context.Request.Path.Value,
            Action: action,
            Result: result,
            ReasonCode: reasonCode,
            StatusCode: context.Response.StatusCode,
            HttpMethod: context.Request.Method,
            Path: context.Request.Path.Value));
    }
}
