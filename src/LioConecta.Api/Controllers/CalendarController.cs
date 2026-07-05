using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/calendar")]
[Authorize]
public sealed class CalendarController(ICalendarService calendarService) : ControllerBase
{
    [HttpGet("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarEventDto>>> GetEvents(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var rangeFrom = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var rangeTo = to ?? DateTimeOffset.UtcNow.AddDays(30);

        var events = await calendarService.GetEventsAsync(rangeFrom, rangeTo, cancellationToken);
        return Ok(events);
    }

    [HttpGet("menu/{date}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CafeteriaMenuDto>> GetMenu(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var menu = await calendarService.GetCafeteriaMenuAsync(date, cancellationToken);
        return menu is null ? NotFound() : Ok(menu);
    }
}
