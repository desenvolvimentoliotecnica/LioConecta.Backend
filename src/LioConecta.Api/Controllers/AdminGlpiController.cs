using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/glpi")]
[Authorize]
[RequirePermission("admin.integrations.test")]
public sealed class AdminGlpiController(IGlpiConfigurationService glpiConfigurationService) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType(typeof(GlpiConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GlpiConnectionTestResponse>> Test(
        [FromBody] TestGlpiConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await glpiConfigurationService.TestConnectionAsync(request, cancellationToken);
        return Ok(result);
    }
}
