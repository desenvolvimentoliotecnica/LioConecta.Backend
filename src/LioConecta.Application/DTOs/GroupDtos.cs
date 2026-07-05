namespace LioConecta.Application.DTOs;

public sealed record GroupDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsPrivate,
    PersonSummaryDto Owner,
    int MemberCount,
    bool IsMember,
    DateTimeOffset CreatedAt);

public sealed record CreateGroupRequest(
    string Name,
    string? Description,
    bool IsPrivate);
