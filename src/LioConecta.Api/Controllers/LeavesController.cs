using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/rh/leave")]
[Authorize]
public sealed class LeavesController(ILeaveService leaveService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(LeaveSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await leaveService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("services")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveServiceDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<LeaveServiceDto>> GetServices()
    {
        return Ok(leaveService.GetServices());
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(LeaveBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var balance = await leaveService.GetBalanceAsync(cancellationToken);
        return Ok(balance);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveHistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeaveHistoryItemDto>>> GetHistory(
        [FromQuery] int limit = 24,
        CancellationToken cancellationToken = default)
    {
        var items = await leaveService.GetHistoryAsync(limit, cancellationToken);
        return Ok(items);
    }

    [HttpGet("requests")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestItemDto>>> GetRequests(
        [FromQuery] int limit = 24,
        CancellationToken cancellationToken = default)
    {
        var items = await leaveService.GetRequestsAsync(limit, cancellationToken);
        return Ok(items);
    }

    [HttpGet("requests/{id:guid}")]
    [ProducesResponseType(typeof(LeaveRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeaveRequestDetailDto>> GetRequestDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await leaveService.GetRequestDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("banco-horas")]
    [ProducesResponseType(typeof(LeaveBancoHorasDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveBancoHorasDto>> GetBancoHoras(CancellationToken cancellationToken)
    {
        var result = await leaveService.GetBancoHorasAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("team-calendar")]
    [ProducesResponseType(typeof(LeaveTeamCalendarDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaveTeamCalendarDto>> GetTeamCalendar(CancellationToken cancellationToken)
    {
        var result = await leaveService.GetTeamCalendarAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("requests")]
    [ProducesResponseType(typeof(LeaveRequestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeaveRequestResultDto>> CreateRequest(
        [FromBody] CreateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await leaveService.CreateRequestAsync(request, cancellationToken);
        return Ok(result);
    }
}
