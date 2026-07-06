using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPlannerService
{
    Task<PlannerTasksResponseDto> GetTasksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlannerBucketDto>> GetBucketsAsync(CancellationToken cancellationToken = default);

    Task<PlannerTaskDto> CreateTaskAsync(CreatePlannerTaskRequest request, CancellationToken cancellationToken = default);

    Task<PlannerTaskDto> UpdateTaskAsync(
        string taskId,
        UpdatePlannerTaskRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(string taskId, CancellationToken cancellationToken = default);
}
