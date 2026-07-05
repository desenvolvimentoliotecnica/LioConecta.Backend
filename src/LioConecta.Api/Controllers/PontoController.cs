using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/rh/ponto")]
[Authorize]
public sealed class PontoController(IPontoService pontoService) : ControllerBase
{
    [HttpGet("periods")]
    [ProducesResponseType(typeof(PontoPeriodSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PontoPeriodSettingsDto>> GetPeriods(CancellationToken cancellationToken)
    {
        var response = await pontoService.GetPeriodSettingsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PontoResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PontoResponseDto>> Get(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var response = await pontoService.GetTimesheetAsync(month, year, cancellationToken);
        return Ok(response);
    }
}
