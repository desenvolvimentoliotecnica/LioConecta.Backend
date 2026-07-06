using System.Globalization;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class PlannerService(
    IPlannerAdapter plannerAdapter,
    IAppSettingsProvider settingsProvider,
    ICurrentUserService currentUserService,
    IPersonRepository personRepository) : IPlannerService
{
    public async Task<PlannerTasksResponseDto> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var usesDevAdapters = settingsProvider.GetBool(AppSettingKeys.IntegrationsUseDevAdapters, true);
        var plannerEnabled = settingsProvider.GetBool(AppSettingKeys.PlannerEnabled, false);
        var planId = settingsProvider.GetString(AppSettingKeys.PlannerPlanId);
        var planTitle = settingsProvider.GetString(AppSettingKeys.PlannerPlanTitle);

        if (!plannerEnabled)
        {
            return new PlannerTasksResponseDto([], usesDevAdapters, false, planTitle);
        }

        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new InvalidOperationException("Planner plan_id não configurado.");
        }

        var viewer = await GetCurrentViewerAsync(cancellationToken);
        var buckets = await plannerAdapter.GetBucketsAsync(planId, cancellationToken);
        var bucketMap = buckets.ToDictionary(b => b.Id, b => b.Name, StringComparer.Ordinal);
        var tasks = await plannerAdapter.GetPlanTasksAsync(planId, cancellationToken);
        var assigneeMap = await ResolveAssigneesAsync(tasks, cancellationToken);

        var mapped = tasks
            .Select(task => MapTask(task, viewer, bucketMap, assigneeMap, planId))
            .OrderByDescending(t => t.DueDate ?? t.StartDate ?? t.CreatedAt)
            .ToList();

        return new PlannerTasksResponseDto(mapped, usesDevAdapters, true, planTitle);
    }

    public async Task<IReadOnlyList<PlannerBucketDto>> GetBucketsAsync(CancellationToken cancellationToken = default)
    {
        EnsurePlannerEnabled();
        var planId = RequirePlanId();
        var buckets = await plannerAdapter.GetBucketsAsync(planId, cancellationToken);
        return buckets
            .Select((bucket, index) => new PlannerBucketDto(bucket.Id, bucket.Name, index))
            .ToList();
    }

    public async Task<PlannerTaskDto> CreateTaskAsync(
        CreatePlannerTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePlannerEnabled();
        var planId = RequirePlanId();
        var viewer = await GetCurrentViewerAsync(cancellationToken);
        var bucketId = await ResolveBucketIdAsync(request.BucketId, planId, cancellationToken);
        var assigneeId = viewer.GraphUserId?.ToString()
            ?? throw new InvalidOperationException("Usuário logado não possui vínculo com Azure AD.");

        var created = await plannerAdapter.CreateTaskAsync(
            planId,
            bucketId,
            request.Title,
            [assigneeId],
            ParseDateTime(request.StartDate),
            ParseDateTime(request.DueDate),
            request.PercentComplete,
            request.Description,
            MapChecklistToGraph(request.Checklist),
            cancellationToken);

        return await MapSingleTaskAsync(created, viewer, planId, cancellationToken);
    }

    public async Task<PlannerTaskDto> UpdateTaskAsync(
        string taskId,
        UpdatePlannerTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePlannerEnabled();
        var planId = RequirePlanId();
        var viewer = await GetCurrentViewerAsync(cancellationToken);
        var existing = await plannerAdapter.GetTaskAsync(taskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarefa {taskId} não encontrada.");

        EnsureCanEdit(existing, viewer);

        var bucketId = string.IsNullOrWhiteSpace(request.BucketId)
            ? null
            : await ResolveBucketIdAsync(request.BucketId, planId, cancellationToken);

        var updated = await plannerAdapter.UpdateTaskAsync(
            taskId,
            request.Title,
            bucketId,
            ParseDateTime(request.StartDate),
            ParseDateTime(request.DueDate),
            request.PercentComplete,
            request.Description,
            MapChecklistToGraph(request.Checklist),
            cancellationToken);

        return await MapSingleTaskAsync(updated, viewer, planId, cancellationToken);
    }

    public async Task DeleteTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        EnsurePlannerEnabled();
        var viewer = await GetCurrentViewerAsync(cancellationToken);
        var existing = await plannerAdapter.GetTaskAsync(taskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tarefa {taskId} não encontrada.");

        EnsureCanEdit(existing, viewer);
        await plannerAdapter.DeleteTaskAsync(taskId, cancellationToken);
    }

    private async Task<PlannerTaskDto> MapSingleTaskAsync(
        PlannerGraphTask task,
        ViewerContextInfo viewer,
        string planId,
        CancellationToken cancellationToken)
    {
        var buckets = await plannerAdapter.GetBucketsAsync(planId, cancellationToken);
        var bucketMap = buckets.ToDictionary(b => b.Id, b => b.Name, StringComparer.Ordinal);
        var assigneeMap = await ResolveAssigneesAsync([task], cancellationToken);
        return MapTask(task, viewer, bucketMap, assigneeMap, planId);
    }

    private void EnsurePlannerEnabled()
    {
        if (!settingsProvider.GetBool(AppSettingKeys.PlannerEnabled, false))
        {
            throw new InvalidOperationException("Integração Planner desabilitada.");
        }
    }

    private string RequirePlanId()
    {
        var planId = settingsProvider.GetString(AppSettingKeys.PlannerPlanId);
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new InvalidOperationException("Planner plan_id não configurado.");
        }

        return planId.Trim();
    }

    private async Task<string> ResolveBucketIdAsync(
        string? requestedBucketId,
        string planId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedBucketId))
        {
            return requestedBucketId.Trim();
        }

        var configured = settingsProvider.GetString(AppSettingKeys.PlannerDefaultBucketId);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        var buckets = await plannerAdapter.GetBucketsAsync(planId, cancellationToken);
        var first = buckets.FirstOrDefault()
            ?? throw new InvalidOperationException("Nenhuma coluna encontrada no plano Planner.");

        return first.Id;
    }

    private static void EnsureCanEdit(PlannerGraphTask task, ViewerContextInfo viewer)
    {
        if (viewer.GraphUserId is null)
        {
            throw new UnauthorizedAccessException("Usuário logado não possui vínculo com Azure AD.");
        }

        var viewerId = viewer.GraphUserId.Value.ToString();
        if (!task.AssigneeIds.Contains(viewerId, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Você só pode editar tarefas atribuídas a você.");
        }
    }

    private async Task<ViewerContextInfo> GetCurrentViewerAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Perfil do usuário logado não encontrado.");

        if (person.AzureAdObjectId is not null)
        {
            return new ViewerContextInfo(person, person.AzureAdObjectId);
        }

        var resolved = await plannerAdapter.ResolveUserByEmailAsync(person.Email, cancellationToken);
        return new ViewerContextInfo(person, resolved?.Id);
    }

    private async Task<IReadOnlyDictionary<string, Person>> ResolveAssigneesAsync(
        IEnumerable<PlannerGraphTask> tasks,
        CancellationToken cancellationToken)
    {
        var assigneeIds = tasks
            .SelectMany(task => task.AssigneeIds)
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .Distinct()
            .ToList();

        if (assigneeIds.Count == 0)
        {
            return new Dictionary<string, Person>();
        }

        var people = await personRepository.GetByAzureObjectIdsAsync(assigneeIds, cancellationToken);
        return people
            .Where(p => p.AzureAdObjectId is not null)
            .ToDictionary(p => p.AzureAdObjectId!.Value.ToString(), p => p, StringComparer.OrdinalIgnoreCase);
    }

    private static PlannerTaskDto MapTask(
        PlannerGraphTask task,
        ViewerContextInfo viewer,
        IReadOnlyDictionary<string, string> bucketMap,
        IReadOnlyDictionary<string, Person> assigneeMap,
        string planId)
    {
        var viewerGraphId = viewer.GraphUserId?.ToString();
        var isOwned = viewerGraphId is not null
            && task.AssigneeIds.Contains(viewerGraphId, StringComparer.OrdinalIgnoreCase);

        var assignees = task.AssigneeIds
            .Select(id =>
            {
                if (assigneeMap.TryGetValue(id, out var person))
                {
                    return new PlannerAssigneeDto(id, person.Name, person.Email);
                }

                return new PlannerAssigneeDto(id, "Colaborador", string.Empty);
            })
            .ToList();

        var bucketName = bucketMap.TryGetValue(task.BucketId, out var name) ? name : "Coluna";
        var groupingStart = task.StartDateTime ?? task.DueDateTime ?? task.CreatedDateTime;
        var groupingEnd = task.DueDateTime ?? task.StartDateTime ?? task.CreatedDateTime;

        return new PlannerTaskDto(
            task.Id,
            task.Title,
            task.Description,
            FormatDateTime(task.StartDateTime ?? groupingStart),
            FormatDateTime(task.DueDateTime ?? groupingEnd),
            task.PercentComplete,
            task.BucketId,
            bucketName,
            assignees,
            task.Checklist
                .Select(item => new PlannerChecklistItemDto(item.Id, item.Title, item.IsChecked))
                .ToList(),
            isOwned,
            isOwned,
            BuildPlannerUrl(planId, task.Id),
            FormatDateTime(task.CreatedDateTime),
            FormatDateTime(task.CompletedDateTime ?? task.CreatedDateTime));
    }

    private static IReadOnlyList<PlannerGraphChecklistItem>? MapChecklistToGraph(
        IReadOnlyList<PlannerChecklistItemDto>? checklist)
    {
        if (checklist is null)
        {
            return null;
        }

        return checklist
            .Select(item => new PlannerGraphChecklistItem
            {
                Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N")[..8] : item.Id,
                Title = item.Text,
                IsChecked = item.Done,
            })
            .ToList();
    }

    private static DateTimeOffset? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }

    private static string FormatDateTime(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string BuildPlannerUrl(string planId, string taskId) =>
        $"https://tasks.office.com/home/planner#/plantaskboard?planId={Uri.EscapeDataString(planId)}&taskId={Uri.EscapeDataString(taskId)}";

    private sealed record ViewerContextInfo(Person Person, Guid? GraphUserId);
}
