using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface IPlannerAdapter
{
    Task<PlannerGraphPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlannerGraphBucket>> GetBucketsAsync(
        string planId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlannerGraphTask>> GetPlanTasksAsync(
        string planId,
        CancellationToken cancellationToken = default);

    Task<PlannerGraphTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default);

    Task<PlannerGraphTask> CreateTaskAsync(
        string planId,
        string bucketId,
        string title,
        IReadOnlyList<string> assigneeIds,
        DateTimeOffset? startDateTime,
        DateTimeOffset? dueDateTime,
        int percentComplete,
        string? description,
        IReadOnlyList<PlannerGraphChecklistItem>? checklist,
        CancellationToken cancellationToken = default);

    Task<PlannerGraphTask> UpdateTaskAsync(
        string taskId,
        string? title,
        string? bucketId,
        DateTimeOffset? startDateTime,
        DateTimeOffset? dueDateTime,
        int? percentComplete,
        string? description,
        IReadOnlyList<PlannerGraphChecklistItem>? checklist,
        CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(string taskId, CancellationToken cancellationToken = default);

    Task<PlannerGraphUser?> ResolveUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}
