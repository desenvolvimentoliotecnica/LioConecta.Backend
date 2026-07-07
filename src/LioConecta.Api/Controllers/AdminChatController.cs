using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/chat")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public sealed class AdminChatController(IChatConfigurationService chatConfigurationService) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType(typeof(ChatConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatConnectionTestResponse>> Test(
        [FromBody] TestChatConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await chatConfigurationService.TestConnectionAsync(request, cancellationToken);
        return Ok(result);
    }
}
