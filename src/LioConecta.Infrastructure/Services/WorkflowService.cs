using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

/// <summary>
/// Workflow MVP (Onda 1B): motor simples de fluxo multi-etapa sequencial guiado por
/// <see cref="WorkflowDefinition.StepsJson"/>. Ver docs/spike-writeback-sql-rm.md.
/// </summary>
public sealed class WorkflowService(
    AppDbContext db,
    IPersonRepository personRepository,
    ICurrentUserService currentUserService) : IWorkflowService
{
    public const string MovimentacaoMeritoKey = "movimentacao-merito";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record StepDefinition(string StepKey, string? AssigneeRole, int Order);

    public async Task<WorkflowInstanceDto> CreateMovimentacaoMeritoAsync(
        CreateMovimentacaoMeritoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var definition = await GetOrSeedDefinitionAsync(MovimentacaoMeritoKey, cancellationToken);
        var subject = await personRepository.GetByIdAsync(request.SubjectPersonId, cancellationToken)
            ?? throw new InvalidOperationException("Colaborador não encontrado.");

        var createdBy = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            DefinitionKey = definition.Key,
            SubjectType = "merit_movement",
            SubjectId = subject.Id,
            Status = "pending",
            PayloadJson = JsonSerializer.Serialize(request, JsonOptions),
            CreatedByPersonId = createdBy,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var stepDefinitions = ParseStepDefinitions(definition.StepsJson);
        foreach (var stepDefinition in stepDefinitions.OrderBy(s => s.Order))
        {
            var assigneePersonId = string.Equals(stepDefinition.AssigneeRole, "Manager", StringComparison.OrdinalIgnoreCase)
                ? subject.ManagerId
                : null;

            instance.Steps.Add(new WorkflowStep
            {
                Id = Guid.NewGuid(),
                InstanceId = instance.Id,
                StepKey = stepDefinition.StepKey,
                Order = stepDefinition.Order,
                AssigneeRole = assigneePersonId is null ? stepDefinition.AssigneeRole : null,
                AssigneePersonId = assigneePersonId,
                Status = "pending",
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(instance, definition.Name, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowInstanceDto>> ListPendingForMeAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        var roleNames = roles.Select(r => r.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var instances = await db.WorkflowInstances
            .Include(i => i.Steps)
            .Where(i => i.Status == "pending")
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var definitions = await db.WorkflowDefinitions.ToDictionaryAsync(d => d.Key, cancellationToken);
        var result = new List<WorkflowInstanceDto>();

        foreach (var instance in instances)
        {
            var activeStep = GetActiveStep(instance);
            if (activeStep is null)
            {
                continue;
            }

            var isAssignee = activeStep.AssigneePersonId == personId
                || (activeStep.AssigneeRole is not null && roleNames.Contains(activeStep.AssigneeRole));

            if (!isAssignee)
            {
                continue;
            }

            var definitionName = definitions.TryGetValue(instance.DefinitionKey, out var def) ? def.Name : instance.DefinitionKey;
            result.Add(await ToDtoAsync(instance, definitionName, cancellationToken));
        }

        return result;
    }

    public async Task<WorkflowInstanceDto?> GetAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await db.WorkflowInstances
            .Include(i => i.Steps)
            .FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return null;
        }

        var definition = await db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Key == instance.DefinitionKey, cancellationToken);
        return await ToDtoAsync(instance, definition?.Name ?? instance.DefinitionKey, cancellationToken);
    }

    public async Task<WorkflowInstanceDto?> ApproveStepAsync(
        Guid instanceId,
        Guid stepId,
        WorkflowDecisionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (instance, step) = await LoadStepForDecisionAsync(instanceId, stepId, cancellationToken);
        if (instance is null || step is null)
        {
            return null;
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        step.Status = "approved";
        step.DecidedBy = personId;
        step.DecidedAt = now;
        step.Comment = request.Comment;
        step.UpdatedAt = now;

        var nextStep = instance.Steps
            .Where(s => s.Order > step.Order)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        instance.Status = nextStep is null ? "approved" : "pending";
        instance.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        var definition = await db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Key == instance.DefinitionKey, cancellationToken);
        return await ToDtoAsync(instance, definition?.Name ?? instance.DefinitionKey, cancellationToken);
    }

    public async Task<WorkflowInstanceDto?> RejectStepAsync(
        Guid instanceId,
        Guid stepId,
        WorkflowDecisionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (instance, step) = await LoadStepForDecisionAsync(instanceId, stepId, cancellationToken);
        if (instance is null || step is null)
        {
            return null;
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        step.Status = "rejected";
        step.DecidedBy = personId;
        step.DecidedAt = now;
        step.Comment = request.Comment;
        step.UpdatedAt = now;

        foreach (var remaining in instance.Steps.Where(s => s.Order > step.Order && s.Status == "pending"))
        {
            remaining.Status = "skipped";
            remaining.UpdatedAt = now;
        }

        instance.Status = "rejected";
        instance.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        var definition = await db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Key == instance.DefinitionKey, cancellationToken);
        return await ToDtoAsync(instance, definition?.Name ?? instance.DefinitionKey, cancellationToken);
    }

    private async Task<(WorkflowInstance? Instance, WorkflowStep? Step)> LoadStepForDecisionAsync(
        Guid instanceId,
        Guid stepId,
        CancellationToken cancellationToken)
    {
        var instance = await db.WorkflowInstances
            .Include(i => i.Steps)
            .FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);

        if (instance is null)
        {
            return (null, null);
        }

        var step = instance.Steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null)
        {
            return (instance, null);
        }

        var activeStep = GetActiveStep(instance);
        if (activeStep is null || activeStep.Id != step.Id)
        {
            throw new InvalidOperationException("Esta etapa não está ativa para decisão no momento.");
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        var roleNames = roles.Select(r => r.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isAssignee = step.AssigneePersonId == personId
            || (step.AssigneeRole is not null && roleNames.Contains(step.AssigneeRole))
            || roleNames.Contains(nameof(UserRole.Admin));

        if (!isAssignee)
        {
            throw new UnauthorizedAccessException("Usuário não é responsável por esta etapa do fluxo.");
        }

        return (instance, step);
    }

    private static WorkflowStep? GetActiveStep(WorkflowInstance instance)
    {
        if (instance.Status != "pending")
        {
            return null;
        }

        return instance.Steps
            .Where(s => s.Status == "pending")
            .OrderBy(s => s.Order)
            .FirstOrDefault();
    }

    private async Task<WorkflowInstanceDto> ToDtoAsync(
        WorkflowInstance instance,
        string definitionName,
        CancellationToken cancellationToken)
    {
        var subject = instance.SubjectType == "merit_movement"
            ? await personRepository.GetByIdAsync(instance.SubjectId, cancellationToken)
            : null;

        var assigneeIds = instance.Steps
            .Where(s => s.AssigneePersonId is not null)
            .Select(s => s.AssigneePersonId!.Value)
            .Distinct()
            .ToList();

        var assignees = assigneeIds.Count > 0
            ? await personRepository.GetByIdsAsync(assigneeIds, cancellationToken)
            : [];
        var assigneesById = assignees.ToDictionary(p => p.Id);

        var steps = instance.Steps
            .OrderBy(s => s.Order)
            .Select(s => new WorkflowStepDto(
                s.Id,
                s.StepKey,
                s.Order,
                s.AssigneeRole,
                s.AssigneePersonId,
                s.AssigneePersonId is not null && assigneesById.TryGetValue(s.AssigneePersonId.Value, out var assignee)
                    ? assignee.Name
                    : null,
                s.Status,
                s.DecidedBy,
                s.DecidedAt,
                s.Comment))
            .ToList();

        return new WorkflowInstanceDto(
            instance.Id,
            instance.DefinitionKey,
            definitionName,
            instance.SubjectType,
            instance.SubjectId,
            subject?.Name,
            instance.Status,
            instance.PayloadJson,
            instance.CreatedByPersonId,
            instance.CreatedAt,
            steps);
    }

    private async Task<WorkflowDefinition> GetOrSeedDefinitionAsync(string key, CancellationToken cancellationToken)
    {
        var definition = await db.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Key == key, cancellationToken);
        if (definition is not null)
        {
            return definition;
        }

        if (key != MovimentacaoMeritoKey)
        {
            throw new InvalidOperationException($"Definição de workflow \"{key}\" não encontrada.");
        }

        var now = DateTimeOffset.UtcNow;
        definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Key = MovimentacaoMeritoKey,
            Name = "Movimentação de mérito",
            StepsJson = JsonSerializer.Serialize(new[]
            {
                new { stepKey = "gestor", assigneeRole = "Manager", order = 1 },
                new { stepKey = "rh", assigneeRole = "HR", order = 2 },
            }),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.WorkflowDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken);
        return definition;
    }

    private static List<StepDefinition> ParseStepDefinitions(string stepsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(stepsJson);
            var steps = new List<StepDefinition>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var stepKey = element.TryGetProperty("stepKey", out var stepKeyEl) ? stepKeyEl.GetString() ?? string.Empty : string.Empty;
                var assigneeRole = element.TryGetProperty("assigneeRole", out var roleEl) ? roleEl.GetString() : null;
                var order = element.TryGetProperty("order", out var orderEl) ? orderEl.GetInt32() : 0;
                if (!string.IsNullOrWhiteSpace(stepKey))
                {
                    steps.Add(new StepDefinition(stepKey, assigneeRole, order));
                }
            }

            return steps;
        }
        catch
        {
            return [];
        }
    }
}
