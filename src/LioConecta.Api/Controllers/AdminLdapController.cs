using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/ldap")]
[Authorize]
[RequirePermission("admin.ldap.test")]
public sealed class AdminLdapController(ILdapConfigurationService ldapConfigurationService) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType(typeof(LdapConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LdapConnectionTestResponse>> Test(
        [FromBody] TestLdapConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ldapConfigurationService.TestConnectionAsync(request, cancellationToken);
        return Ok(result);
    }
}
