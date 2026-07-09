using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/graph")]
[Authorize]
[RequirePermission("admin.integrations.test")]
public sealed class AdminGraphController(IGraphConfigurationService graphConfigurationService) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType(typeof(GraphConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GraphConnectionTestResponse>> Test(
        [FromBody] TestGraphConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await graphConfigurationService.TestConnectionAsync(request, cancellationToken);
        return Ok(result);
    }
}
