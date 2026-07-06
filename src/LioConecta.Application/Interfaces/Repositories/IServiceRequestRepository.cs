using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IServiceRequestRepository
{
    Task<IReadOnlyList<ServiceRequest>> GetByRequesterAsync(Guid requesterId, CancellationToken cancellationToken = default);

    Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(ServiceRequest request, CancellationToken cancellationToken = default);

    Task AddEventAsync(ServiceRequestEvent serviceRequestEvent, CancellationToken cancellationToken = default);

    Task UpdateAsync(ServiceRequest request, CancellationToken cancellationToken = default);

    Task SetExternalRefAsync(
        Guid id,
        string externalRef,
        string assigneeTeam,
        CancellationToken cancellationToken = default);
}
