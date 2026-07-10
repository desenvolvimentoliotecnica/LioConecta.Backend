using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IGroupService
{
    Task ExpireOverdueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupDto>> GetMyGroupsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupDto>> GetPendingForMeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupDto>> GetExpiredAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupDto>> GetExploreGroupsAsync(CancellationToken cancellationToken = default);

    Task<GroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);

    Task<GroupDto?> ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto?> RejectAsync(Guid id, RejectGroupRequest request, CancellationToken cancellationToken = default);

    Task<GroupDto?> ResubmitAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto?> JoinAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto?> LeaveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupDto?> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupMemberDto>> GetMembersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GroupMemberDto?> UpdateMemberRoleAsync(
        Guid groupId,
        Guid memberId,
        UpdateGroupMemberRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupWallPostDto>> GetWallAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<GroupWallPostDto> CreateWallPostAsync(
        Guid groupId,
        CreateGroupWallPostRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteWallPostAsync(Guid groupId, Guid postId, CancellationToken cancellationToken = default);

    Task<GroupWallPostDto?> ToggleWallReactionAsync(
        Guid groupId,
        Guid postId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupTopicSummaryDto>> GetTopicsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task<GroupTopicDetailDto?> GetTopicAsync(
        Guid groupId,
        Guid topicId,
        CancellationToken cancellationToken = default);

    Task<GroupTopicDetailDto> CreateTopicAsync(
        Guid groupId,
        CreateGroupTopicRequest request,
        CancellationToken cancellationToken = default);

    Task<GroupTopicReplyDto> CreateTopicReplyAsync(
        Guid groupId,
        Guid topicId,
        CreateGroupTopicReplyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTopicAsync(Guid groupId, Guid topicId, CancellationToken cancellationToken = default);

    Task<bool> DeleteTopicReplyAsync(
        Guid groupId,
        Guid topicId,
        Guid replyId,
        CancellationToken cancellationToken = default);

    Task<GroupOwnershipTransferDto> RequestOwnershipTransferAsync(
        Guid groupId,
        CreateOwnershipTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupOwnershipTransferDto>> GetPendingOwnershipTransfersForMeAsync(
        CancellationToken cancellationToken = default);

    Task<GroupOwnershipTransferDto?> ApproveOwnershipTransferAsync(
        Guid transferId,
        CancellationToken cancellationToken = default);

    Task<GroupOwnershipTransferDto?> RejectOwnershipTransferAsync(
        Guid transferId,
        RejectGroupRequest request,
        CancellationToken cancellationToken = default);
}
