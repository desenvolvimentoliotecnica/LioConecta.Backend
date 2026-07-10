using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

/// <summary>
/// Etapa de aprovação de uma <see cref="WorkflowInstance"/>.
/// </summary>
public class WorkflowStep : BaseEntity
{
    public Guid InstanceId { get; set; }

    public WorkflowInstance? Instance { get; set; }

    public string StepKey { get; set; } = string.Empty;

    public int Order { get; set; }

    /// <summary>Papel responsável pela etapa (ex.: "Manager", "HR"). Nulo quando atribuído a pessoa específica.</summary>
    public string? AssigneeRole { get; set; }

    public Guid? AssigneePersonId { get; set; }

    /// <summary>"pending" | "approved" | "rejected" | "skipped".</summary>
    public string Status { get; set; } = "pending";

    public Guid? DecidedBy { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? Comment { get; set; }
}
