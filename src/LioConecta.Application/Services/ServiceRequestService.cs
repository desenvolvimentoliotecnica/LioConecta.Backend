using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class ServiceRequestService(
    IServiceRequestRepository serviceRequestRepository,
    ICurrentUserService currentUserService) : IServiceRequestService
{
    public async Task<IReadOnlyList<ServiceRequestDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var requesterId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var requests = await serviceRequestRepository.GetByRequesterAsync(requesterId, cancellationToken);
        return requests.Select(ServiceRequestMapper.ToDto).ToList();
    }

    public async Task<ServiceRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requesterId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var request = await serviceRequestRepository.GetByIdAsync(id, cancellationToken);
        if (request is null || request.RequesterId != requesterId)
        {
            return null;
        }

        return ServiceRequestMapper.ToDto(request);
    }

    public async Task<ServiceRequestDto> CreateAsync(
        CreateServiceRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var requesterId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var serviceRequest = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            Type = request.Type.Trim(),
            Category = request.Category,
            Status = ServiceRequestStatus.Submitted,
            RequesterId = requesterId,
            PayloadJson = JsonMapper.SerializeObjectDictionary(request.Payload),
            CreatedAt = now,
            UpdatedAt = now
        };

        await serviceRequestRepository.AddAsync(serviceRequest, cancellationToken);

        var submittedEvent = new ServiceRequestEvent
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequest.Id,
            EventType = "Submitted",
            ActorId = requesterId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await serviceRequestRepository.AddEventAsync(submittedEvent, cancellationToken);
        serviceRequest.Events = [submittedEvent];

        return ServiceRequestMapper.ToDto(serviceRequest);
    }
}
