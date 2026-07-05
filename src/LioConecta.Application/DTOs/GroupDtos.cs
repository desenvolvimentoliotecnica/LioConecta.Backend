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
    bool IsMember,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason);

public sealed record CreateGroupRequest(
    string Name,
    string? Description,
    GroupType Type,
    GroupAccessMode AccessMode,
    string Icon);

public sealed record RejectGroupRequest(string? Reason);
