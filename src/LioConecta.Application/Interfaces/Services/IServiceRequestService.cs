using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IServiceRequestService
{
    Task<IReadOnlyList<ServiceRequestDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceRequestDto> CreateAsync(CreateServiceRequestRequest request, CancellationToken cancellationToken = default);
}
