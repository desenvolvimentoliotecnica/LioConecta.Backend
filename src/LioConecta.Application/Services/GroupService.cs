using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

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

    public async Task<IReadOnlyList<GroupDto>> GetPendingApprovalAsync(CancellationToken cancellationToken = default)
    {
        var groups = await groupRepository.GetPendingApprovalAsync(cancellationToken);
        return groups.Select(g => GroupMapper.ToDto(g, isMember: false)).ToList();
    }

    public async Task<IReadOnlyList<GroupDto>> GetExploreGroupsAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var groups = await groupRepository.GetActiveForExploreAsync(cancellationToken);
        var result = new List<GroupDto>();

        foreach (var group in groups)
        {
            var isMember = group.Members.Any(m => m.PersonId == personId);
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
        var icon = string.IsNullOrWhiteSpace(request.Icon) ? "fa-users" : request.Icon.Trim();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            AccessMode = request.AccessMode,
            Icon = icon,
            Status = GroupStatus.PendingApproval,
            IsPrivate = request.AccessMode == GroupAccessMode.Private,
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

        var loaded = await groupRepository.GetByIdAsync(group.Id, cancellationToken);
        return GroupMapper.ToDto(loaded ?? group, isMember: true);
    }

    public async Task<GroupDto?> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reviewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null || group.Status != GroupStatus.PendingApproval)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        group.Status = GroupStatus.Active;
        group.ReviewedById = reviewerId;
        group.ReviewedAt = now;
        group.RejectionReason = null;
        await groupRepository.UpdateAsync(group, cancellationToken);

        var isMember = await groupRepository.IsMemberAsync(id, reviewerId, cancellationToken);
        return GroupMapper.ToDto(group, isMember);
    }

    public async Task<GroupDto?> RejectAsync(
        Guid id,
        RejectGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null || group.Status != GroupStatus.PendingApproval)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        group.Status = GroupStatus.Rejected;
        group.ReviewedById = reviewerId;
        group.ReviewedAt = now;
        group.RejectionReason = request.Reason?.Trim();
        await groupRepository.UpdateAsync(group, cancellationToken);

        var isMember = await groupRepository.IsMemberAsync(id, reviewerId, cancellationToken);
        return GroupMapper.ToDto(group, isMember);
    }
}
