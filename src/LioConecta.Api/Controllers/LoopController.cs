using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/loop")]
[Authorize]
public sealed class LoopController(ILoopService loopService) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(LoopBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoopBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
    {
        var bootstrap = await loopService.GetBootstrapAsync(cancellationToken);
        return Ok(bootstrap);
    }
}
