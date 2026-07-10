using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

/// <summary>
/// Definição de um fluxo de aprovação reutilizável (workflow MVP). Ex.: "movimentacao-merito".
/// </summary>
public class WorkflowDefinition : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>JSON: [{ "stepKey": "gestor", "assigneeRole": "Manager", "order": 1 }, ...]</summary>
    public string StepsJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;
}
