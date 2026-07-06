namespace LioConecta.Application.DTOs;

public sealed record PlannerAssigneeDto(
    string Id,
    string Name,
    string Email);

public sealed record PlannerChecklistItemDto(
    string Id,
    string Text,
    bool Done);

public sealed record PlannerTaskDto(
    string Id,
    string Title,
    string Description,
    string? StartDate,
    string? DueDate,
    int PercentComplete,
    string BucketId,
    string BucketName,
    IReadOnlyList<PlannerAssigneeDto> Assignees,
    IReadOnlyList<PlannerChecklistItemDto> Checklist,
    bool IsOwnedByCurrentUser,
    bool CanEdit,
    string PlannerUrl,
    string CreatedAt,
    string UpdatedAt);

public sealed record PlannerTasksResponseDto(
    IReadOnlyList<PlannerTaskDto> Tasks,
    bool UsesDevAdapters,
    bool PlannerEnabled,
    string? PlanTitle);

public sealed record PlannerBucketDto(
    string Id,
    string Name,
    int OrderHint);

public sealed record CreatePlannerTaskRequest(
    string Title,
    string? Description,
    string? StartDate,
    string? DueDate,
    int PercentComplete,
    string? BucketId,
    IReadOnlyList<PlannerChecklistItemDto>? Checklist);

public sealed record UpdatePlannerTaskRequest(
    string Title,
    string? Description,
    string? StartDate,
    string? DueDate,
    int PercentComplete,
    string? BucketId,
    IReadOnlyList<PlannerChecklistItemDto>? Checklist);

public sealed record TestPlannerConnectionRequest(
    string? PlanId);

public sealed record PlannerConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail,
    bool UsesDevAdapters,
    bool PlannerEnabled,
    string? PlanId,
    string? PlanTitle,
    int? BucketCount,
    int? TaskCount);
