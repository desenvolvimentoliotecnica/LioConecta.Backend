using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/compass")]
[Authorize]
public sealed class CompassController(ICompassService compassService) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(CompassBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompassBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
        => Ok(await compassService.GetBootstrapAsync(cancellationToken));

    [HttpGet("meta")]
    [ProducesResponseType(typeof(CompassMetaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompassMetaDto>> GetMeta(CancellationToken cancellationToken)
        => Ok(await compassService.GetMetaAsync(cancellationToken));

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(CompassDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompassDashboardDto>> GetDashboard(
        [FromQuery] CompassYtdQuery query,
        CancellationToken cancellationToken)
        => Ok(await compassService.GetDashboardAsync(query, cancellationToken));

    [HttpGet("ytd")]
    [ProducesResponseType(typeof(CompassYtdPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompassYtdPageDto>> GetYtd(
        [FromQuery] CompassYtdQuery query,
        CancellationToken cancellationToken)
        => Ok(await compassService.GetYtdPageAsync(query, cancellationToken));

    [HttpGet("aggregates")]
    [ProducesResponseType(typeof(CompassAggregatesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompassAggregatesDto>> GetAggregates(
        [FromQuery] CompassAggregatesQuery query,
        CancellationToken cancellationToken)
        => Ok(await compassService.GetAggregatesAsync(query, cancellationToken));

    [HttpGet("scenarios")]
    [ProducesResponseType(typeof(CompassScenariosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompassScenariosDto>> GetScenarios(
        [FromQuery] CompassScenariosQuery query,
        CancellationToken cancellationToken)
        => Ok(await compassService.GetScenariosAsync(query, cancellationToken));

    [HttpGet("scenarios/{id}/rows")]
    [ProducesResponseType(typeof(CompassScenarioRowsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompassScenarioRowsPageDto>> GetScenarioRows(
        [FromRoute] string id,
        [FromQuery] CompassScenarioRowsQuery query,
        CancellationToken cancellationToken)
    {
        var page = await compassService.GetScenarioRowsAsync(id, query, cancellationToken);
        if (string.Equals(page.Message, "Cenário não encontrado.", StringComparison.Ordinal))
        {
            return NotFound(page);
        }

        return Ok(page);
    }
}
