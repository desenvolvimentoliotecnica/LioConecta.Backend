using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/planner")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public sealed class AdminPlannerController(IPlannerConfigurationService plannerConfigurationService) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType(typeof(PlannerConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlannerConnectionTestResponse>> Test(
        [FromBody] TestPlannerConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await plannerConfigurationService.TestConnectionAsync(request, cancellationToken);
        return Ok(result);
    }
}
