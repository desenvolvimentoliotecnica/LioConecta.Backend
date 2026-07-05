using LioConecta.Application.Common;
using LioConecta.Application.Common.Observability;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace LioConecta.Api.Auth;

public sealed class ObservabilityAuthorizationResultHandler(
    IAppSettingsProvider settings,
    IServiceScopeFactory scopeFactory) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded &&
            settings.GetBool(AppSettingKeys.ObservabilityAccessAuditEnabled, true))
        {
            using var scope = scopeFactory.CreateScope();
            var recorder = scope.ServiceProvider.GetRequiredService<IAccessAuditRecorder>();
            var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

            var eventName = authorizeResult.Challenged
                ? ObservabilityEventNames.Authentication.AnonymousBlocked
                : ObservabilityEventNames.Authorization.AccessDenied;

            var result = authorizeResult.Challenged
                ? AccessEventResults.Failed
                : AccessEventResults.Denied;

            var reasonCode = authorizeResult.Challenged ? "challenge" : "forbidden";

            await AccessAuditMiddleware.RecordAccessAsync(
                context,
                recorder,
                currentUserService,
                eventName,
                resource: context.Request.Path.Value,
                action: context.GetEndpoint()?.DisplayName,
                result: result,
                reasonCode: reasonCode);
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
