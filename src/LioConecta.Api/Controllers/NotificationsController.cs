using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var page = await notificationService.GetNotificationsAsync(
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(count);
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }
}
