using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IGroupRepository
{
    Task<IReadOnlyList<Group>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetPendingApprovalAsync(CancellationToken cancellationToken = default);

    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Group?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Group group, CancellationToken cancellationToken = default);

    Task AddMemberAsync(GroupMember member, CancellationToken cancellationToken = default);

    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(Guid groupId, Guid personId, CancellationToken cancellationToken = default);
}
