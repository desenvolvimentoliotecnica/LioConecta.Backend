using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/activities")]
[Authorize]
public sealed class ActivitiesController(IActivityService activityService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetActivities(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var activities = await activityService.GetRecentAsync(limit, cancellationToken);
        return Ok(activities);
    }
}
