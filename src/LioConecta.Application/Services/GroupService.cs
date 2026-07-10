using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class GroupService(
    IGroupRepository groupRepository,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    IPermissionService permissionService,
    INotificationService notificationService) : IGroupService
{
    public static readonly TimeSpan ApprovalTtl = TimeSpan.FromDays(14);

    public async Task ExpireOverdueAsync(CancellationToken cancellationToken = default)
    {
        var expired = await groupRepository.ExpireOverduePendingAsync(DateTimeOffset.UtcNow, cancellationToken);
        foreach (var group in expired)
        {
            await notificationService.NotifyGroupCreationExpiredAsync(
                group.OwnerId,
                group.Id,
                group.Name,
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<GroupDto>> GetMyGroupsAsync(CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var groups = await groupRepository.GetByPersonIdAsync(personId, cancellationToken);
        return groups.Select(g => Map(g, personId)).ToList();
    }

    public async Task<IReadOnlyList<GroupDto>> GetPendingForMeAsync(CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var groups = await groupRepository.GetPendingForApproverAsync(personId, cancellationToken);
        return groups.Select(g => Map(g, personId)).ToList();
    }

    public async Task<IReadOnlyList<GroupDto>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        if (!await permissionService.HasPermissionAsync("groups.approve", cancellationToken: cancellationToken))
        {
            throw new UnauthorizedAccessException("Sem permissão para listar grupos expirados.");
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var groups = await groupRepository.GetExpiredAsync(cancellationToken);
        return groups.Select(g => Map(g, personId)).ToList();
    }

    public async Task<IReadOnlyList<GroupDto>> GetExploreGroupsAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var groups = await groupRepository.GetActiveForExploreAsync(cancellationToken);
        return groups.Select(g => Map(g, personId)).ToList();
    }

    public async Task<GroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdAsync(id, cancellationToken);
        return group is null ? null : Map(group, personId);
    }

    public async Task<GroupDto> CreateAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var ownerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var owner = await personRepository.GetByIdAsync(ownerId, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa não encontrada.");

        if (owner.ManagerId is null)
        {
            throw new InvalidOperationException(
                "Não é possível criar um grupo sem gestor direto cadastrado. Atualize seu organograma ou fale com o RH.");
        }

        var now = DateTimeOffset.UtcNow;
        var icon = string.IsNullOrWhiteSpace(request.Icon) ? "fa-users" : request.Icon.Trim();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            AccessMode = GroupAccessMode.Open,
            Icon = icon,
            Status = GroupStatus.PendingApproval,
            IsPrivate = false,
            OwnerId = ownerId,
            ApproverId = owner.ManagerId,
            SubmittedAt = now,
            ExpiresAt = now.Add(ApprovalTtl),
            ResubmitCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        await groupRepository.AddAsync(group, cancellationToken);
        await groupRepository.AddMemberAsync(new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            PersonId = ownerId,
            Role = GroupMemberRole.Owner,
            JoinedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await notificationService.NotifyGroupCreationRequestedAsync(
            owner.ManagerId.Value,
            group.Id,
            group.Name,
            owner.Name,
            cancellationToken);

        var loaded = await groupRepository.GetByIdAsync(group.Id, cancellationToken);
        return Map(loaded ?? group, ownerId);
    }

    public async Task<GroupDto?> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var reviewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null)
        {
            return null;
        }

        if (!await CanApproveGroupAsync(group, reviewerId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Você não pode aprovar este grupo.");
        }

        var now = DateTimeOffset.UtcNow;
        group.Status = GroupStatus.Active;
        group.ReviewedById = reviewerId;
        group.ReviewedAt = now;
        group.RejectionReason = null;
        await groupRepository.UpdateAsync(group, cancellationToken);

        await notificationService.NotifyGroupCreationDecisionAsync(
            group.OwnerId,
            group.Id,
            group.Name,
            approved: true,
            reason: null,
            cancellationToken);

        return Map(group, reviewerId);
    }

    public async Task<GroupDto?> RejectAsync(
        Guid id,
        RejectGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var reviewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null)
        {
            return null;
        }

        if (!await CanApproveGroupAsync(group, reviewerId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Você não pode rejeitar este grupo.");
        }

        var now = DateTimeOffset.UtcNow;
        group.Status = GroupStatus.Rejected;
        group.ReviewedById = reviewerId;
        group.ReviewedAt = now;
        group.RejectionReason = request.Reason?.Trim();
        await groupRepository.UpdateAsync(group, cancellationToken);

        await notificationService.NotifyGroupCreationDecisionAsync(
            group.OwnerId,
            group.Id,
            group.Name,
            approved: false,
            reason: group.RejectionReason,
            cancellationToken);

        return Map(group, reviewerId);
    }

    public async Task<GroupDto?> ResubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await ExpireOverdueAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null || group.OwnerId != personId || group.Status != GroupStatus.Expired)
        {
            return null;
        }

        var owner = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa não encontrada.");
        if (owner.ManagerId is null)
        {
            throw new InvalidOperationException(
                "Não é possível reenviar sem gestor direto cadastrado.");
        }

        var now = DateTimeOffset.UtcNow;
        group.Status = GroupStatus.PendingApproval;
        group.ApproverId = owner.ManagerId;
        group.SubmittedAt = now;
        group.ExpiresAt = now.Add(ApprovalTtl);
        group.ResubmitCount += 1;
        group.ReviewedAt = null;
        group.ReviewedById = null;
        group.RejectionReason = null;
        await groupRepository.UpdateAsync(group, cancellationToken);

        await notificationService.NotifyGroupCreationRequestedAsync(
            owner.ManagerId.Value,
            group.Id,
            group.Name,
            owner.Name,
            cancellationToken);

        return Map(group, personId);
    }

    public async Task<GroupDto?> JoinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null || group.Status != GroupStatus.Active)
        {
            return null;
        }

        var existing = await groupRepository.GetMembershipAsync(id, personId, cancellationToken);
        if (existing is null)
        {
            var now = DateTimeOffset.UtcNow;
            await groupRepository.AddMemberAsync(new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = id,
                PersonId = personId,
                Role = GroupMemberRole.Member,
                JoinedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
            group = await groupRepository.GetByIdAsync(id, cancellationToken) ?? group;
        }

        return Map(group, personId);
    }

    public async Task<GroupDto?> LeaveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null)
        {
            return null;
        }

        var membership = await groupRepository.GetMembershipAsync(id, personId, cancellationToken);
        if (membership is null)
        {
            return Map(group, personId);
        }

        if (membership.Role == GroupMemberRole.Owner)
        {
            throw new InvalidOperationException(
                "O dono não pode sair do grupo. Transfira a direção antes de sair.");
        }

        await groupRepository.RemoveMemberAsync(membership, cancellationToken);
        group = await groupRepository.GetByIdAsync(id, cancellationToken) ?? group;
        return Map(group, personId);
    }

    public async Task<GroupDto?> UpdateAsync(
        Guid id,
        UpdateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null)
        {
            return null;
        }

        if (group.OwnerId != personId)
        {
            throw new UnauthorizedAccessException("Apenas o dono pode editar o grupo.");
        }

        group.Name = request.Name.Trim();
        group.Description = request.Description?.Trim();
        group.Type = request.Type;
        group.Icon = string.IsNullOrWhiteSpace(request.Icon) ? group.Icon : request.Icon.Trim();
        group.IsPrivate = false;
        group.AccessMode = GroupAccessMode.Open;
        await groupRepository.UpdateAsync(group, cancellationToken);
        return Map(group, personId);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (group is null)
        {
            return false;
        }

        if (group.OwnerId != personId)
        {
            throw new UnauthorizedAccessException("Apenas o dono pode excluir o grupo.");
        }

        await groupRepository.DeleteAsync(group, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<GroupMemberDto>> GetMembersAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var group = await groupRepository.GetByIdAsync(id, cancellationToken);
        if (group is null || group.Status != GroupStatus.Active)
        {
            throw new KeyNotFoundException("Grupo não encontrado.");
        }

        var members = await groupRepository.GetMembersAsync(id, cancellationToken);
        return members.Select(GroupMapper.ToMemberDto).ToList();
    }

    public async Task<GroupMemberDto?> UpdateMemberRoleAsync(
        Guid groupId,
        Guid memberId,
        UpdateGroupMemberRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(groupId, cancellationToken);
        if (group is null || group.Status != GroupStatus.Active)
        {
            return null;
        }

        if (group.OwnerId != personId)
        {
            throw new UnauthorizedAccessException("Apenas o dono pode alterar papéis.");
        }

        if (request.Role == GroupMemberRole.Owner)
        {
            throw new InvalidOperationException("Use a transferência de dono para atribuir a direção.");
        }

        var member = await groupRepository.GetMemberByIdAsync(groupId, memberId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        if (member.Role == GroupMemberRole.Owner)
        {
            throw new InvalidOperationException("Não é possível alterar o papel do dono por este endpoint.");
        }

        member.Role = request.Role;
        await groupRepository.UpdateMemberAsync(member, cancellationToken);
        return GroupMapper.ToMemberDto(member);
    }

    public async Task<IReadOnlyList<GroupWallPostDto>> GetWallAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveMemberAsync(groupId, cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var posts = await groupRepository.GetWallPostsAsync(groupId, cancellationToken);
        return posts.Select(p => GroupMapper.ToWallPostDto(p, personId)).ToList();
    }

    public async Task<GroupWallPostDto> CreateWallPostAsync(
        Guid groupId,
        CreateGroupWallPostRequest request,
        CancellationToken cancellationToken = default)
    {
        var membership = await EnsureActiveMemberAsync(groupId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            throw new InvalidOperationException("Informe texto ou imagem para o mural.");
        }

        var now = DateTimeOffset.UtcNow;
        var post = new GroupPost
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            AuthorId = membership.PersonId,
            Content = request.Content?.Trim() ?? string.Empty,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await groupRepository.AddWallPostAsync(post, cancellationToken);

        var author = await personRepository.GetByIdAsync(membership.PersonId, cancellationToken);
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is not null && author is not null)
        {
            var memberIds = (await groupRepository.GetMembersAsync(groupId, cancellationToken))
                .Select(m => m.PersonId)
                .Where(id => id != membership.PersonId)
                .ToList();
            await notificationService.NotifyGroupWallPostAsync(
                memberIds,
                groupId,
                group.Name,
                author.Name,
                cancellationToken);
        }

        var loaded = await groupRepository.GetWallPostAsync(groupId, post.Id, cancellationToken) ?? post;
        return GroupMapper.ToWallPostDto(loaded, membership.PersonId);
    }

    public async Task<bool> DeleteWallPostAsync(
        Guid groupId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var membership = await EnsureActiveMemberAsync(groupId, cancellationToken);
        var post = await groupRepository.GetWallPostAsync(groupId, postId, cancellationToken);
        if (post is null)
        {
            return false;
        }

        if (post.AuthorId != membership.PersonId
            && membership.Role is not (GroupMemberRole.Owner or GroupMemberRole.Moderator))
        {
            throw new UnauthorizedAccessException("Sem permissão para apagar este post.");
        }

        await groupRepository.DeleteWallPostAsync(post, cancellationToken);
        return true;
    }

    public async Task<GroupWallPostDto?> ToggleWallReactionAsync(
        Guid groupId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var membership = await EnsureActiveMemberAsync(groupId, cancellationToken);
        var post = await groupRepository.GetWallPostAsync(groupId, postId, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var existing = await groupRepository.GetReactionAsync(postId, membership.PersonId, cancellationToken);
        if (existing is null)
        {
            var now = DateTimeOffset.UtcNow;
            await groupRepository.AddReactionAsync(new GroupPostReaction
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                PersonId = membership.PersonId,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
        }
        else
        {
            await groupRepository.RemoveReactionAsync(existing, cancellationToken);
        }

        var refreshed = await groupRepository.GetWallPostAsync(groupId, postId, cancellationToken);
        return refreshed is null ? null : GroupMapper.ToWallPostDto(refreshed, membership.PersonId);
    }

    public async Task<IReadOnlyList<GroupTopicSummaryDto>> GetTopicsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveMemberAsync(groupId, cancellationToken);
        var topics = await groupRepository.GetTopicsAsync(groupId, cancellationToken);
        return topics.Select(GroupMapper.ToTopicSummaryDto).ToList();
    }

    public async Task<GroupTopicDetailDto?> GetTopicAsync(
        Guid groupId,
        Guid topicId,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveMemberAsync(groupId, cancellationToken);
        var topic = await groupRepository.GetTopicAsync(groupId, topicId, cancellationToken);
        return topic is null ? null : GroupMapper.ToTopicDetailDto(topic);
    }

    public async Task<GroupTopicDetailDto> CreateTopicAsync(
        Guid groupId,
        CreateGroupTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var membership = await EnsureActiveMemberAsync(groupId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            throw new InvalidOperationException("Título e corpo do tópico são obrigatórios.");
        }

        var now = DateTimeOffset.UtcNow;
        var topic = new GroupTopic
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            AuthorId = membership.PersonId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            LastActivityAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        await groupRepository.AddTopicAsync(topic, cancellationToken);

        var author = await personRepository.GetByIdAsync(membership.PersonId, cancellationToken);
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is not null && author is not null)
        {
            var memberIds = (await groupRepository.GetMembersAsync(groupId, cancellationToken))
                .Select(m => m.PersonId)
                .Where(id => id != membership.PersonId)
                .ToList();
            await notificationService.NotifyGroupTopicCreatedAsync(
                memberIds,
                groupId,
                topic.Id,
                group.Name,
                topic.Title,
                author.Name,
                cancellationToken);
        }

        var loaded = await groupRepository.GetTopicAsync(groupId, topic.Id, cancellationToken) ?? topic;
        return GroupMapper.ToTopicDetailDto(loaded);
    }

    public async Task<GroupTopicReplyDto> CreateTopicReplyAsync(
        Guid groupId,
        Guid topicId,
        CreateGroupTopicReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var membership = await EnsureActiveMemberAsync(groupId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new InvalidOperationException("A resposta não pode ser vazia.");
        }

        var topic = await groupRepository.GetTopicAsync(groupId, topicId, cancellationToken)
            ?? throw new KeyNotFoundException("Tópico não encontrado.");

        var now = DateTimeOffset.UtcNow;
        var reply = new GroupTopicReply
        {
            Id = Guid.NewGuid(),
            TopicId = topicId,
            AuthorId = membership.PersonId,
            Body = request.Body.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await groupRepository.AddTopicReplyAsync(reply, cancellationToken);

        topic.LastActivityAt = now;
        await groupRepository.UpdateTopicAsync(topic, cancellationToken);

        var author = await personRepository.GetByIdAsync(membership.PersonId, cancellationToken);
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is not null && author is not null)
        {
            var participantIds = new HashSet<Guid> { topic.AuthorId };
            foreach (var existing in topic.Replies ?? [])
            {
                participantIds.Add(existing.AuthorId);
            }

            participantIds.Remove(membership.PersonId);
            await notificationService.NotifyGroupTopicReplyAsync(
                participantIds.ToList(),
                groupId,
                topicId,
                group.Name,
                topic.Title,
                author.Name,
                cancellationToken);
        }

        var loaded = await groupRepository.GetTopicReplyAsync(topicId, reply.Id, cancellationToken) ?? reply;
        return GroupMapper.ToTopicReplyDto(loaded);
    }

    public async Task<bool> DeleteTopicAsync(
        Guid groupId,
        Guid topicId,
        CancellationToken cancellationToken = default)
    {
        var membership = await EnsureActiveMemberAsync(groupId, cancellationToken);
        var topic = await groupRepository.GetTopicAsync(groupId, topicId, cancellationToken);
        if (topic is null)
        {
            return false;
        }

        if (topic.AuthorId != membership.PersonId
            && membership.Role is not (GroupMemberRole.Owner or GroupMemberRole.Moderator))
        {
            throw new UnauthorizedAccessException("Sem permissão para apagar este tópico.");
        }

        await groupRepository.DeleteTopicAsync(topic, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteTopicReplyAsync(
        Guid groupId,
        Guid topicId,
        Guid replyId,
        CancellationToken cancellationToken = default)
    {
        var membership = await EnsureActiveMemberAsync(groupId, cancellationToken);
        var reply = await groupRepository.GetTopicReplyAsync(topicId, replyId, cancellationToken);
        if (reply is null)
        {
            return false;
        }

        if (reply.AuthorId != membership.PersonId
            && membership.Role is not (GroupMemberRole.Owner or GroupMemberRole.Moderator))
        {
            throw new UnauthorizedAccessException("Sem permissão para apagar esta resposta.");
        }

        await groupRepository.DeleteTopicReplyAsync(reply, cancellationToken);
        return true;
    }

    public async Task<GroupOwnershipTransferDto> RequestOwnershipTransferAsync(
        Guid groupId,
        CreateOwnershipTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdForUpdateAsync(groupId, cancellationToken)
            ?? throw new KeyNotFoundException("Grupo não encontrado.");

        if (group.OwnerId != personId || group.Status != GroupStatus.Active)
        {
            throw new UnauthorizedAccessException("Apenas o dono de um grupo ativo pode transferir a direção.");
        }

        if (request.ToPersonId == personId)
        {
            throw new InvalidOperationException("Escolha outra pessoa para a direção do grupo.");
        }

        if (await groupRepository.HasPendingOwnershipTransferAsync(groupId, cancellationToken))
        {
            throw new InvalidOperationException("Já existe uma transferência pendente para este grupo.");
        }

        var toPerson = await personRepository.GetByIdAsync(request.ToPersonId, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa destino não encontrada.");
        if (toPerson.ManagerId is null)
        {
            throw new InvalidOperationException(
                "O novo dono precisa ter gestor direto cadastrado para aprovar a transferência.");
        }

        var membership = await groupRepository.GetMembershipAsync(groupId, request.ToPersonId, cancellationToken);
        if (membership is null)
        {
            throw new InvalidOperationException("O novo dono precisa ser membro do grupo.");
        }

        var fromOwner = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Dono atual não encontrado.");

        var now = DateTimeOffset.UtcNow;
        var transfer = new GroupOwnershipTransfer
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            FromOwnerId = personId,
            ToPersonId = request.ToPersonId,
            ApproverId = toPerson.ManagerId,
            Status = GroupOwnershipTransferStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        await groupRepository.AddOwnershipTransferAsync(transfer, cancellationToken);

        await notificationService.NotifyGroupOwnershipTransferRequestedAsync(
            request.ToPersonId,
            toPerson.ManagerId.Value,
            groupId,
            group.Name,
            fromOwner.Name,
            toPerson.Name,
            cancellationToken);

        var loaded = await groupRepository.GetOwnershipTransferAsync(transfer.Id, cancellationToken) ?? transfer;
        return GroupMapper.ToOwnershipTransferDto(loaded);
    }

    public async Task<IReadOnlyList<GroupOwnershipTransferDto>> GetPendingOwnershipTransfersForMeAsync(
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var transfers = await groupRepository.GetPendingOwnershipTransfersForApproverAsync(personId, cancellationToken);
        return transfers.Select(GroupMapper.ToOwnershipTransferDto).ToList();
    }

    public async Task<GroupOwnershipTransferDto?> ApproveOwnershipTransferAsync(
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var transfer = await groupRepository.GetOwnershipTransferAsync(transferId, cancellationToken);
        if (transfer is null || transfer.Status != GroupOwnershipTransferStatus.Pending)
        {
            return null;
        }

        if (transfer.ApproverId != personId)
        {
            throw new UnauthorizedAccessException("Apenas o gestor do novo dono pode aprovar a transferência.");
        }

        var group = await groupRepository.GetByIdForUpdateAsync(transfer.GroupId, cancellationToken)
            ?? throw new KeyNotFoundException("Grupo não encontrado.");

        var now = DateTimeOffset.UtcNow;
        var oldOwnerMembership = await groupRepository.GetMembershipAsync(
            group.Id, transfer.FromOwnerId, cancellationToken);
        if (oldOwnerMembership is not null)
        {
            oldOwnerMembership.Role = GroupMemberRole.Moderator;
            await groupRepository.UpdateMemberAsync(oldOwnerMembership, cancellationToken);
        }

        var newOwnerMembership = await groupRepository.GetMembershipAsync(
            group.Id, transfer.ToPersonId, cancellationToken);
        if (newOwnerMembership is null)
        {
            await groupRepository.AddMemberAsync(new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                PersonId = transfer.ToPersonId,
                Role = GroupMemberRole.Owner,
                JoinedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
        }
        else
        {
            newOwnerMembership.Role = GroupMemberRole.Owner;
            await groupRepository.UpdateMemberAsync(newOwnerMembership, cancellationToken);
        }

        group.OwnerId = transfer.ToPersonId;
        await groupRepository.UpdateAsync(group, cancellationToken);

        transfer.Status = GroupOwnershipTransferStatus.Approved;
        transfer.ReviewedAt = now;
        await groupRepository.UpdateOwnershipTransferAsync(transfer, cancellationToken);

        await notificationService.NotifyGroupOwnershipTransferDecisionAsync(
            [transfer.FromOwnerId, transfer.ToPersonId],
            transfer.GroupId,
            group.Name,
            approved: true,
            reason: null,
            cancellationToken);

        var loaded = await groupRepository.GetOwnershipTransferAsync(transferId, cancellationToken) ?? transfer;
        return GroupMapper.ToOwnershipTransferDto(loaded);
    }

    public async Task<GroupOwnershipTransferDto?> RejectOwnershipTransferAsync(
        Guid transferId,
        RejectGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var transfer = await groupRepository.GetOwnershipTransferAsync(transferId, cancellationToken);
        if (transfer is null || transfer.Status != GroupOwnershipTransferStatus.Pending)
        {
            return null;
        }

        if (transfer.ApproverId != personId)
        {
            throw new UnauthorizedAccessException("Apenas o gestor do novo dono pode rejeitar a transferência.");
        }

        transfer.Status = GroupOwnershipTransferStatus.Rejected;
        transfer.ReviewedAt = DateTimeOffset.UtcNow;
        transfer.RejectionReason = request.Reason?.Trim();
        await groupRepository.UpdateOwnershipTransferAsync(transfer, cancellationToken);

        await notificationService.NotifyGroupOwnershipTransferDecisionAsync(
            [transfer.FromOwnerId, transfer.ToPersonId],
            transfer.GroupId,
            transfer.Group?.Name ?? "Grupo",
            approved: false,
            reason: transfer.RejectionReason,
            cancellationToken);

        return GroupMapper.ToOwnershipTransferDto(transfer);
    }

    private async Task<bool> CanApproveGroupAsync(
        Group group,
        Guid reviewerId,
        CancellationToken cancellationToken)
    {
        if (group.Status == GroupStatus.PendingApproval && group.ApproverId == reviewerId)
        {
            return true;
        }

        if (group.Status == GroupStatus.Expired
            && await permissionService.HasPermissionAsync("groups.approve", cancellationToken: cancellationToken))
        {
            return true;
        }

        return false;
    }

    private async Task<GroupMember> EnsureActiveMemberAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new KeyNotFoundException("Grupo não encontrado.");
        if (group.Status != GroupStatus.Active)
        {
            throw new InvalidOperationException("O grupo não está ativo.");
        }

        var membership = await groupRepository.GetMembershipAsync(groupId, personId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Você precisa participar do grupo para esta ação.");
        return membership;
    }

    private static GroupDto Map(Group group, Guid personId)
    {
        var membership = group.Members?.FirstOrDefault(m => m.PersonId == personId);
        var isMember = membership is not null;
        return GroupMapper.ToDto(group, isMember, membership?.Role);
    }
}
