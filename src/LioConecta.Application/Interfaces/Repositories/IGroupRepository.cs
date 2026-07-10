using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IGroupRepository
{
    Task<IReadOnlyList<Group>> ExpireOverduePendingAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetPendingForApproverAsync(Guid approverId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetExpiredAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Group>> GetActiveForExploreAsync(CancellationToken cancellationToken = default);

    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Group?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Group group, CancellationToken cancellationToken = default);

    Task AddMemberAsync(GroupMember member, CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(GroupMember member, CancellationToken cancellationToken = default);

    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);

    Task DeleteAsync(Group group, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(Guid groupId, Guid personId, CancellationToken cancellationToken = default);

    Task<GroupMember?> GetMembershipAsync(Guid groupId, Guid personId, CancellationToken cancellationToken = default);

    Task<GroupMember?> GetMemberByIdAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task UpdateMemberAsync(GroupMember member, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupPost>> GetWallPostsAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<GroupPost?> GetWallPostAsync(Guid groupId, Guid postId, CancellationToken cancellationToken = default);

    Task AddWallPostAsync(GroupPost post, CancellationToken cancellationToken = default);

    Task DeleteWallPostAsync(GroupPost post, CancellationToken cancellationToken = default);

    Task<GroupPostReaction?> GetReactionAsync(Guid postId, Guid personId, CancellationToken cancellationToken = default);

    Task AddReactionAsync(GroupPostReaction reaction, CancellationToken cancellationToken = default);

    Task RemoveReactionAsync(GroupPostReaction reaction, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupTopic>> GetTopicsAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<GroupTopic?> GetTopicAsync(Guid groupId, Guid topicId, CancellationToken cancellationToken = default);

    Task AddTopicAsync(GroupTopic topic, CancellationToken cancellationToken = default);

    Task UpdateTopicAsync(GroupTopic topic, CancellationToken cancellationToken = default);

    Task DeleteTopicAsync(GroupTopic topic, CancellationToken cancellationToken = default);

    Task AddTopicReplyAsync(GroupTopicReply reply, CancellationToken cancellationToken = default);

    Task<GroupTopicReply?> GetTopicReplyAsync(Guid topicId, Guid replyId, CancellationToken cancellationToken = default);

    Task DeleteTopicReplyAsync(GroupTopicReply reply, CancellationToken cancellationToken = default);

    Task AddOwnershipTransferAsync(GroupOwnershipTransfer transfer, CancellationToken cancellationToken = default);

    Task<GroupOwnershipTransfer?> GetOwnershipTransferAsync(Guid transferId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupOwnershipTransfer>> GetPendingOwnershipTransfersForApproverAsync(
        Guid approverId,
        CancellationToken cancellationToken = default);

    Task UpdateOwnershipTransferAsync(GroupOwnershipTransfer transfer, CancellationToken cancellationToken = default);

    Task<bool> HasPendingOwnershipTransferAsync(Guid groupId, CancellationToken cancellationToken = default);
}
