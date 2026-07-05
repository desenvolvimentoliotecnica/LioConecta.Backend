using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/me/preferences")]
[Authorize]
public sealed class PreferencesController(IUserPreferenceService userPreferenceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferencesDto>> Get(CancellationToken cancellationToken)
    {
        var preferences = await userPreferenceService.GetAsync(cancellationToken);
        return Ok(preferences);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferencesDto>> Update(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = await userPreferenceService.UpdateAsync(request, cancellationToken);
        return Ok(preferences);
    }
}
