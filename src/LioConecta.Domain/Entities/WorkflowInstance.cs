using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

/// <summary>
/// Instância em execução de um <see cref="WorkflowDefinition"/> para um assunto do portal
/// (ex.: movimentação de mérito de uma pessoa).
/// </summary>
public class WorkflowInstance : BaseEntity
{
    public string DefinitionKey { get; set; } = string.Empty;

    /// <summary>Tipo do assunto, ex.: "merit_movement".</summary>
    public string SubjectType { get; set; } = string.Empty;

    public Guid SubjectId { get; set; }

    /// <summary>"pending" | "approved" | "rejected" | "cancelled".</summary>
    public string Status { get; set; } = "pending";

    /// <summary>JSON com o payload de negócio da solicitação (ex.: pessoa, cargo, novo salário).</summary>
    public string PayloadJson { get; set; } = "{}";

    public Guid CreatedByPersonId { get; set; }

    public List<WorkflowStep> Steps { get; set; } = [];
}
