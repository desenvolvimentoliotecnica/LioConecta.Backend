using LioConecta.Api.Auth;
using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/integration")]
[Authorize(AuthenticationSchemes = $"{IntegrationApiKeyDefaults.SchemeName},{JwtBearerDefaults.AuthenticationScheme}")]
public sealed class IntegrationController(IPortalIntegrationService integrationService) : ControllerBase
{
    [HttpPost("feed/publish")]
    [RequirePermission("portal.integration.feed.write")]
    public async Task<ActionResult<IntegrationFeedPublishResponse>> PublishFeed(
        [FromBody] IntegrationFeedPublishRequest request,
        CancellationToken cancellationToken)
        => Ok(await integrationService.PublishFeedAsync(request, cancellationToken));

    [HttpPost("notifications/notify")]
    [RequirePermission("portal.integration.notification.write")]
    public async Task<ActionResult<IntegrationNotifyResponse>> Notify(
        [FromBody] IntegrationNotifyRequest request,
        CancellationToken cancellationToken)
        => Ok(await integrationService.NotifyAsync(request, cancellationToken));

    [HttpPost("email/enqueue")]
    [RequirePermission("portal.integration.email.enqueue")]
    public async Task<ActionResult<IntegrationEmailEnqueueResponse>> EnqueueEmail(
        [FromBody] IntegrationEmailEnqueueRequest request,
        CancellationToken cancellationToken)
        => Ok(await integrationService.EnqueueEmailAsync(request, cancellationToken));

    [HttpPost("people/resolve")]
    [RequirePermission("portal.integration.people.resolve")]
    public async Task<ActionResult<IntegrationPeopleResolveResponse>> ResolvePeople(
        [FromBody] IntegrationPeopleResolveRequest request,
        CancellationToken cancellationToken)
        => Ok(await integrationService.ResolvePeopleAsync(request, cancellationToken));
}
