using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/mood")]
[Authorize]
public sealed class MoodCheckController(IMoodCheckService moodCheckService) : ControllerBase
{
    [HttpGet("today")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MoodTodayDto>> GetToday(CancellationToken cancellationToken)
    {
        var result = await moodCheckService.GetTodayAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterMoodRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await moodCheckService.RegisterAsync(request, cancellationToken);
            return Created(string.Empty, result);
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { message = "Você já registrou seu humor hoje." });
        }
    }

    [HttpGet("metrics")]
    [RequirePermission("mood.analytics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MoodMetricsDto>> GetMetrics(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var metrics = await moodCheckService.GetMetricsAsync(from, to, cancellationToken);
        return Ok(metrics);
    }
}
