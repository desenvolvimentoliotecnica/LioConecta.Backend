using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IServiceRequestRepository
{
    Task<IReadOnlyList<ServiceRequest>> GetByRequesterAsync(Guid requesterId, CancellationToken cancellationToken = default);

    Task<ServiceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceRequest?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> ListManagementAsync(
        IReadOnlyList<string> types,
        ServiceRequestStatus? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(ServiceRequest request, CancellationToken cancellationToken = default);

    Task AddEventAsync(ServiceRequestEvent serviceRequestEvent, CancellationToken cancellationToken = default);

    Task UpdateAsync(ServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza status/updatedAt sem grafo rastreado e grava o evento em seguida.
    /// Evita DbUpdateConcurrencyException ao incluir Requester/Actor no aggregate.
    /// </summary>
    Task UpdateStatusAndAddEventAsync(
        Guid id,
        ServiceRequestStatus status,
        ServiceRequestEvent serviceRequestEvent,
        CancellationToken cancellationToken = default);

    Task SetExternalRefAsync(
        Guid id,
        string externalRef,
        string assigneeTeam,
        CancellationToken cancellationToken = default);
}
