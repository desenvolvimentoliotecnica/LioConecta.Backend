using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IGroupService
{
    Task<IReadOnlyList<GroupDto>> GetMyGroupsAsync(CancellationToken cancellationToken = default);

    Task<GroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);
}
