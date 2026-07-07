using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/calendar")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public sealed class AdminCalendarController(ICalendarConfigurationService calendarConfigurationService) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType(typeof(CalendarConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalendarConnectionTestResponse>> Test(
        [FromBody] TestCalendarConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await calendarConfigurationService.TestConnectionAsync(
            request ?? new TestCalendarConnectionRequest(null),
            cancellationToken);
        return Ok(result);
    }
}
