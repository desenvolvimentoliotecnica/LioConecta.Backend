using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/totvs-rm")]
[Authorize]
[RequirePermission("admin.totvs.manage")]
public sealed class AdminTotvsRmController(ITotvsRmConfigurationService totvsRmConfigurationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(TotvsRmConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TotvsRmConfigurationDto>> Get(CancellationToken cancellationToken)
    {
        var result = await totvsRmConfigurationService.GetAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(TotvsRmConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TotvsRmConfigurationDto>> Save(
        [FromBody] UpsertTotvsRmConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await totvsRmConfigurationService.SaveAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("test")]
    [ProducesResponseType(typeof(TotvsRmConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TotvsRmConnectionTestResponse>> Test(
        [FromBody] UpsertTotvsRmConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await totvsRmConfigurationService.TestConnectionAsync(request, cancellationToken);
        return Ok(result);
    }
}
