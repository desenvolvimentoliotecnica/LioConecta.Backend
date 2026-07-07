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
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(CalendarBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalendarBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
    {
        var bootstrap = await calendarService.GetBootstrapAsync(cancellationToken);
        return Ok(bootstrap);
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(CalendarStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalendarStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await calendarService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("link-account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LinkAccount(
        [FromBody] LinkCalendarAccountRequest request,
        CancellationToken cancellationToken)
    {
        await calendarService.LinkAccountAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("link-account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlinkAccount(CancellationToken cancellationToken)
    {
        await calendarService.UnlinkAccountAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("calendars")]
    [ProducesResponseType(typeof(IReadOnlyList<CalendarListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarListItemDto>>> GetCalendars(
        CancellationToken cancellationToken)
    {
        var calendars = await calendarService.GetCalendarsAsync(cancellationToken);
        return Ok(calendars);
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(IReadOnlyList<CalendarEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarEventDto>>> GetEvents(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string[]? calendarIds,
        CancellationToken cancellationToken = default)
    {
        var rangeFrom = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var rangeTo = to ?? DateTimeOffset.UtcNow.AddDays(30);

        var events = await calendarService.GetEventsAsync(
            rangeFrom,
            rangeTo,
            calendarIds,
            cancellationToken);

        return Ok(events);
    }

    [HttpPost("events")]
    [ProducesResponseType(typeof(CalendarEventDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CalendarEventDto>> CreateEvent(
        [FromBody] CreateCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        var created = await calendarService.CreateEventAsync(request, cancellationToken);
        return Created(string.Empty, created);
    }

    [HttpPatch("events/{eventId}")]
    [ProducesResponseType(typeof(CalendarEventDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalendarEventDto>> UpdateEvent(
        string eventId,
        [FromBody] UpdateCalendarEventRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await calendarService.UpdateEventAsync(eventId, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("events/{eventId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEvent(string eventId, CancellationToken cancellationToken)
    {
        await calendarService.DeleteEventAsync(eventId, cancellationToken);
        return NoContent();
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
