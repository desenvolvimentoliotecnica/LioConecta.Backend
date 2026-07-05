using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class GroupRepository(AppDbContext db) : IGroupRepository
{
    public Task<IReadOnlyList<Group>> GetByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.GroupMembers
            .Where(m => m.PersonId == personId)
            .Include(m => m.Group)!.ThenInclude(g => g!.Owner)
            .Select(m => m.Group!)
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Group>)t.Result, cancellationToken);

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Members).ThenInclude(m => m.Person)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMemberAsync(GroupMember member, CancellationToken cancellationToken = default)
    {
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsMemberAsync(
        Guid groupId,
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.PersonId == personId, cancellationToken);
}
