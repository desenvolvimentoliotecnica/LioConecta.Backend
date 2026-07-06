using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/ti/help-desk")]
[Authorize]
public sealed class HelpDeskController(IHelpDeskService helpDeskService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(HelpDeskSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HelpDeskSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await helpDeskService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("services")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskServiceDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<HelpDeskServiceDto>> GetServices()
    {
        return Ok(helpDeskService.GetServices());
    }

    [HttpGet("knowledge")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskKnowledgeArticleDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<HelpDeskKnowledgeArticleDto>> GetKnowledge(
        [FromQuery] string? q,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Ok(helpDeskService.GetKnowledge(q));
    }

    [HttpPost("tickets")]
    [ProducesResponseType(typeof(HelpDeskTicketResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HelpDeskTicketResultDto>> CreateTicket(
        [FromBody] CreateHelpDeskTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await helpDeskService.CreateTicketAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("tickets/mine")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskTicketListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HelpDeskTicketListItemDto>>> GetMyTickets(
        [FromQuery] string scope = "open",
        CancellationToken cancellationToken = default)
    {
        var tickets = await helpDeskService.GetMyTicketsAsync(scope, cancellationToken);
        return Ok(tickets);
    }

    [HttpGet("tickets/all")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskTicketListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<HelpDeskTicketListItemDto>>> GetAllTickets(
        [FromQuery] string scope = "open",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tickets = await helpDeskService.GetAllTicketsAsync(scope, cancellationToken);
            return Ok(tickets);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("tickets/{ticketId}")]
    [ProducesResponseType(typeof(HelpDeskTicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HelpDeskTicketDetailDto>> GetTicketDetail(
        string ticketId,
        CancellationToken cancellationToken)
    {
        var detail = await helpDeskService.GetTicketDetailAsync(ticketId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }
}
