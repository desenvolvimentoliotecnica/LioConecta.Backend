using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class GroupService(
    IGroupRepository groupRepository,
    ICurrentUserService currentUserService) : IGroupService
{
    public async Task<IReadOnlyList<GroupDto>> GetMyGroupsAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var groups = await groupRepository.GetByPersonIdAsync(personId, cancellationToken);
        var result = new List<GroupDto>();

        foreach (var group in groups)
        {
            var isMember = await groupRepository.IsMemberAsync(group.Id, personId, cancellationToken);
            result.Add(GroupMapper.ToDto(group, isMember));
        }

        return result;
    }

    public async Task<GroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdAsync(id, cancellationToken);
        if (group is null)
        {
            return null;
        }

        var isMember = await groupRepository.IsMemberAsync(id, personId, cancellationToken);
        return GroupMapper.ToDto(group, isMember);
    }

    public async Task<GroupDto> CreateAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsPrivate = request.IsPrivate,
            OwnerId = ownerId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await groupRepository.AddAsync(group, cancellationToken);
        await groupRepository.AddMemberAsync(new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            PersonId = ownerId,
            JoinedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        group.Members = [new GroupMember { PersonId = ownerId }];
        return GroupMapper.ToDto(group, isMember: true);
    }
}
