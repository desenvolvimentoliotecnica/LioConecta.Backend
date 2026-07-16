using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPortalIntegrationService
{
    Task<IntegrationFeedPublishResponse> PublishFeedAsync(
        IntegrationFeedPublishRequest request,
        CancellationToken cancellationToken = default);

    Task<IntegrationNotifyResponse> NotifyAsync(
        IntegrationNotifyRequest request,
        CancellationToken cancellationToken = default);

    Task<IntegrationEmailEnqueueResponse> EnqueueEmailAsync(
        IntegrationEmailEnqueueRequest request,
        CancellationToken cancellationToken = default);

    Task<IntegrationPeopleResolveResponse> ResolvePeopleAsync(
        IntegrationPeopleResolveRequest request,
        CancellationToken cancellationToken = default);
}
