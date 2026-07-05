using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IGroupService
{
    Task<IReadOnlyList<GroupDto>> GetMyGroupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupDto>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);

    Task<GroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);

    Task<GroupDto?> ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto?> RejectAsync(Guid id, RejectGroupRequest request, CancellationToken cancellationToken = default);
}
