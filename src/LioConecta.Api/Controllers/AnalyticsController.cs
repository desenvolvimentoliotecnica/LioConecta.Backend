using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public sealed class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await analyticsService.GetDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("snapshot")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsSnapshotDto>> GetSnapshot(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var snapshot = await analyticsService.GetSnapshotAsync(period, cancellationToken);
        return Ok(snapshot);
    }
}
