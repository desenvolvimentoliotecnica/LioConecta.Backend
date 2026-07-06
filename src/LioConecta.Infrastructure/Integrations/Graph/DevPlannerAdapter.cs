using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class DevPlannerAdapter : IPlannerAdapter
{
    private const string PlanId = "i8VPy5W0q0O5Ag52QcdRMWUAH9Yn";
    private static readonly Guid LeonardoObjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb103");
    private static readonly Guid CarlosObjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb102");

    private readonly List<PlannerGraphTask> _tasks =
    [
        new()
        {
            Id = "planner-task-001",
            PlanId = PlanId,
            BucketId = "bucket-doing",
            Title = "Revisar backlog do trimestre",
            PercentComplete = 45,
            StartDateTime = DateTimeOffset.UtcNow.AddDays(-2),
            DueDateTime = DateTimeOffset.UtcNow.AddDays(5),
            CreatedDateTime = DateTimeOffset.UtcNow.AddDays(-3),
            AssigneeIds = [LeonardoObjectId.ToString()],
            Description = "Consolidar entregas do Q3 com stakeholders de Produto e Sistemas.",
            Checklist =
            [
                new PlannerGraphChecklistItem { Id = "chk-1", Title = "Levantar pendências", IsChecked = true },
                new PlannerGraphChecklistItem { Id = "chk-2", Title = "Validar com gestores", IsChecked = false },
            ],
            Etag = "W/\"dev-1\"",
        },
        new()
        {
            Id = "planner-task-002",
            PlanId = PlanId,
            BucketId = "bucket-todo",
            Title = "Atualizar documentação de onboarding",
            PercentComplete = 0,
            StartDateTime = DateTimeOffset.UtcNow,
            DueDateTime = DateTimeOffset.UtcNow.AddDays(12),
            CreatedDateTime = DateTimeOffset.UtcNow.AddDays(-1),
            AssigneeIds = [LeonardoObjectId.ToString()],
            Description = "Revisar fluxo de integração no hub Documentos.",
            Checklist =
            [
                new PlannerGraphChecklistItem { Id = "chk-3", Title = "Comparar com versão anterior", IsChecked = false },
            ],
            Etag = "W/\"dev-2\"",
        },
        new()
        {
            Id = "planner-task-003",
            PlanId = PlanId,
            BucketId = "bucket-done",
            Title = "Validar integração Totvs",
            PercentComplete = 100,
            StartDateTime = DateTimeOffset.UtcNow.AddDays(-10),
            DueDateTime = DateTimeOffset.UtcNow.AddDays(-2),
            CreatedDateTime = DateTimeOffset.UtcNow.AddDays(-12),
            CompletedDateTime = DateTimeOffset.UtcNow.AddDays(-2),
            AssigneeIds = [CarlosObjectId.ToString()],
            Description = "Smoke test dos endpoints de holerite e ponto.",
            Checklist = [],
            Etag = "W/\"dev-3\"",
        },
    ];

    public Task<PlannerGraphPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default) =>
        Task.FromResult<PlannerGraphPlan?>(new PlannerGraphPlan
        {
            Id = planId,
            Title = "Teste Leo (mock)",
        });

    public Task<IReadOnlyList<PlannerGraphBucket>> GetBucketsAsync(
        string planId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PlannerGraphBucket>>(new List<PlannerGraphBucket>
        {
            new() { Id = "bucket-todo", Name = "A fazer", OrderHint = " !" },
            new() { Id = "bucket-doing", Name = "Em andamento", OrderHint = " !!" },
            new() { Id = "bucket-done", Name = "Concluído", OrderHint = " !!!" },
        });

    public Task<IReadOnlyList<PlannerGraphTask>> GetPlanTasksAsync(
        string planId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PlannerGraphTask>>(_tasks.Where(t => t.PlanId == planId).ToList());

    public Task<PlannerGraphTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tasks.FirstOrDefault(t => t.Id == taskId));

    public Task<PlannerGraphTask> CreateTaskAsync(
        string planId,
        string bucketId,
        string title,
        IReadOnlyList<string> assigneeIds,
        DateTimeOffset? startDateTime,
        DateTimeOffset? dueDateTime,
        int percentComplete,
        string? description,
        IReadOnlyList<PlannerGraphChecklistItem>? checklist,
        CancellationToken cancellationToken = default)
    {
        var task = new PlannerGraphTask
        {
            Id = $"planner-task-{Guid.NewGuid():N}"[..20],
            PlanId = planId,
            BucketId = bucketId,
            Title = title,
            PercentComplete = percentComplete,
            StartDateTime = startDateTime,
            DueDateTime = dueDateTime,
            CreatedDateTime = DateTimeOffset.UtcNow,
            AssigneeIds = assigneeIds.ToList(),
            Description = description ?? string.Empty,
            Checklist = checklist?.ToList() ?? [],
            Etag = "W/\"dev-new\"",
        };
        _tasks.Insert(0, task);
        return Task.FromResult(task);
    }

    public Task<PlannerGraphTask> UpdateTaskAsync(
        string taskId,
        string? title,
        string? bucketId,
        DateTimeOffset? startDateTime,
        DateTimeOffset? dueDateTime,
        int? percentComplete,
        string? description,
        IReadOnlyList<PlannerGraphChecklistItem>? checklist,
        CancellationToken cancellationToken = default)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new InvalidOperationException($"Planner task {taskId} was not found.");

        if (!string.IsNullOrWhiteSpace(title))
        {
            task.Title = title;
        }

        if (!string.IsNullOrWhiteSpace(bucketId))
        {
            task.BucketId = bucketId;
        }

        if (startDateTime.HasValue)
        {
            task.StartDateTime = startDateTime;
        }

        if (dueDateTime.HasValue)
        {
            task.DueDateTime = dueDateTime;
        }

        if (percentComplete.HasValue)
        {
            task.PercentComplete = percentComplete.Value;
        }

        if (description is not null)
        {
            task.Description = description;
        }

        if (checklist is not null)
        {
            task.Checklist = checklist.ToList();
        }

        return Task.FromResult(task);
    }

    public Task DeleteTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _tasks.RemoveAll(t => t.Id == taskId);
        return Task.CompletedTask;
    }

    public Task<PlannerGraphUser?> ResolveUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (email.Contains("leonardo.mendes", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<PlannerGraphUser?>(new PlannerGraphUser
            {
                Id = LeonardoObjectId,
                DisplayName = "Leonardo Sabino Mendes",
                Mail = "leonardo.mendes@liotecnica.com.br",
                UserPrincipalName = "leonardo.mendes@liotecnica.com.br",
            });
        }

        if (email.Contains("carlos.mendes", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<PlannerGraphUser?>(new PlannerGraphUser
            {
                Id = CarlosObjectId,
                DisplayName = "Carlos Mendes",
                Mail = "carlos.mendes@liotecnica.com.br",
                UserPrincipalName = "carlos.mendes@liotecnica.com.br",
            });
        }

        return Task.FromResult<PlannerGraphUser?>(null);
    }
}
