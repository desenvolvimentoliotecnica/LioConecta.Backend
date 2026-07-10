using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record GroupDto(
    Guid Id,
    string Name,
    string? Description,
    GroupType Type,
    GroupAccessMode AccessMode,
    string Icon,
    GroupStatus Status,
    bool IsPrivate,
    PersonSummaryDto Owner,
    int MemberCount,
    int PostCount,
    int TopicCount,
    bool IsMember,
    GroupMemberRole? MyRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    PersonSummaryDto? Approver);

public sealed record CreateGroupRequest(
    string Name,
    string? Description,
    GroupType Type,
    string Icon);

public sealed record UpdateGroupRequest(
    string Name,
    string? Description,
    GroupType Type,
    string Icon);

public sealed record RejectGroupRequest(string? Reason);

public sealed record GroupMemberDto(
    Guid Id,
    PersonSummaryDto Person,
    GroupMemberRole Role,
    DateTimeOffset JoinedAt);

public sealed record UpdateGroupMemberRoleRequest(GroupMemberRole Role);

public sealed record CreateGroupWallPostRequest(string Content, string? ImageUrl);

public sealed record GroupWallPostDto(
    Guid Id,
    PersonSummaryDto Author,
    string Content,
    string? ImageUrl,
    int ReactionCount,
    bool ReactedByMe,
    DateTimeOffset CreatedAt);

public sealed record CreateGroupTopicRequest(string Title, string Body);

public sealed record GroupTopicSummaryDto(
    Guid Id,
    string Title,
    PersonSummaryDto Author,
    int ReplyCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt);

public sealed record GroupTopicReplyDto(
    Guid Id,
    PersonSummaryDto Author,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record GroupTopicDetailDto(
    Guid Id,
    string Title,
    string Body,
    PersonSummaryDto Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    IReadOnlyList<GroupTopicReplyDto> Replies);

public sealed record CreateGroupTopicReplyRequest(string Body);

public sealed record CreateOwnershipTransferRequest(Guid ToPersonId);

public sealed record GroupOwnershipTransferDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    PersonSummaryDto FromOwner,
    PersonSummaryDto ToPerson,
    PersonSummaryDto? Approver,
    GroupOwnershipTransferStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason);
