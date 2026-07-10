using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

/// <summary>
/// Fluxos de aprovação RH (Workflow MVP — Onda 1B). Ver docs/spike-writeback-sql-rm.md.
/// </summary>
[ApiController]
[Route("api/v1/rh/workflows")]
[Authorize]
public sealed class WorkflowsController(IWorkflowService workflowService) : ControllerBase
{
    [HttpPost("movimentacao-merito")]
    [RequirePermission("rh_requests.manage")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkflowInstanceDto>> CreateMovimentacaoMerito(
        [FromBody] CreateMovimentacaoMeritoRequestDto request,
        CancellationToken cancellationToken)
    {
        var instance = await workflowService.CreateMovimentacaoMeritoAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = instance.Id }, instance);
    }

    [HttpGet("pending-for-me")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowInstanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkflowInstanceDto>>> ListPendingForMe(CancellationToken cancellationToken)
    {
        var instances = await workflowService.ListPendingForMeAsync(cancellationToken);
        return Ok(instances);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowInstanceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var instance = await workflowService.GetAsync(id, cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }

    [HttpPost("{id:guid}/steps/{stepId:guid}/approve")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkflowInstanceDto>> ApproveStep(
        Guid id,
        Guid stepId,
        [FromBody] WorkflowDecisionRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var instance = await workflowService.ApproveStepAsync(
                id,
                stepId,
                request ?? new WorkflowDecisionRequestDto(null),
                cancellationToken);
            return instance is null ? NotFound() : Ok(instance);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/steps/{stepId:guid}/reject")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkflowInstanceDto>> RejectStep(
        Guid id,
        Guid stepId,
        [FromBody] WorkflowDecisionRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var instance = await workflowService.RejectStepAsync(
                id,
                stepId,
                request ?? new WorkflowDecisionRequestDto(null),
                cancellationToken);
            return instance is null ? NotFound() : Ok(instance);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { detail = ex.Message });
        }
    }
}
