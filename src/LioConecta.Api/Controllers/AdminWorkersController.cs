using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/workers")]
[Authorize]
[RequirePermission("admin.workers.manage")]
public sealed class AdminWorkersController(IWorkerTriggerService workerTriggerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkerDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkerDefinitionDto>>> List(CancellationToken cancellationToken)
    {
        var workers = await workerTriggerService.ListWorkersAsync(cancellationToken);
        return Ok(workers);
    }

    [HttpGet("{workerKey}/runs")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkerRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkerRunDto>>> ListRuns(
        string workerKey,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var runs = await workerTriggerService.ListRunsAsync(workerKey, limit, cancellationToken);
        return Ok(runs);
    }

    [HttpGet("{workerKey}/runs/{runId:guid}")]
    [ProducesResponseType(typeof(WorkerRunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkerRunDetailDto>> GetRun(
        string workerKey,
        Guid runId,
        CancellationToken cancellationToken)
    {
        var run = await workerTriggerService.GetRunAsync(runId, cancellationToken);
        if (run is null || !string.Equals(run.Run.WorkerKey, workerKey, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        return Ok(run);
    }

    [HttpPost("{workerKey}/trigger")]
    [ProducesResponseType(typeof(WorkerTriggerResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkerTriggerResultDto>> Trigger(
        string workerKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await workerTriggerService.TriggerAsync(workerKey, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
