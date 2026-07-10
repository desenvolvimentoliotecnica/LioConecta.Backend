using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

/// <summary>
/// Workflow MVP (Onda 1B): fluxos de aprovação multi-etapa genéricos, iniciando com a
/// definição seed "movimentacao-merito" (gestor -> RH). Ver docs/spike-writeback-sql-rm.md.
/// </summary>
public interface IWorkflowService
{
    Task<WorkflowInstanceDto> CreateMovimentacaoMeritoAsync(
        CreateMovimentacaoMeritoRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Lista instâncias com etapa pendente atribuída ao usuário atual (por pessoa ou por role).</summary>
    Task<IReadOnlyList<WorkflowInstanceDto>> ListPendingForMeAsync(CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDto?> GetAsync(Guid instanceId, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDto?> ApproveStepAsync(
        Guid instanceId,
        Guid stepId,
        WorkflowDecisionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDto?> RejectStepAsync(
        Guid instanceId,
        Guid stepId,
        WorkflowDecisionRequestDto request,
        CancellationToken cancellationToken = default);
}
