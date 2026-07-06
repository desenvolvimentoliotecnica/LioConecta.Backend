using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/planner")]
[Authorize]
public sealed class PlannerController(IPlannerService plannerService) : ControllerBase
{
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(PlannerTasksResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlannerTasksResponseDto>> GetTasks(CancellationToken cancellationToken)
    {
        var result = await plannerService.GetTasksAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("buckets")]
    [ProducesResponseType(typeof(IReadOnlyList<PlannerBucketDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlannerBucketDto>>> GetBuckets(CancellationToken cancellationToken)
    {
        var buckets = await plannerService.GetBucketsAsync(cancellationToken);
        return Ok(buckets);
    }

    [HttpPost("tasks")]
    [ProducesResponseType(typeof(PlannerTaskDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlannerTaskDto>> CreateTask(
        [FromBody] CreatePlannerTaskRequest request,
        CancellationToken cancellationToken)
    {
        var created = await plannerService.CreateTaskAsync(request, cancellationToken);
        return Ok(created);
    }

    [HttpPatch("tasks/{taskId}")]
    [ProducesResponseType(typeof(PlannerTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlannerTaskDto>> UpdateTask(
        string taskId,
        [FromBody] UpdatePlannerTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await plannerService.UpdateTaskAsync(taskId, request, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }

    [HttpDelete("tasks/{taskId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(string taskId, CancellationToken cancellationToken)
    {
        try
        {
            await plannerService.DeleteTaskAsync(taskId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
    }
}
