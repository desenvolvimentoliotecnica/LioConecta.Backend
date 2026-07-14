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
    public async Task<ActionResult<IReadOnlyList<HelpDeskKnowledgeArticleDto>>> GetKnowledge(
        [FromQuery] string? q,
        CancellationToken cancellationToken = default)
    {
        return Ok(await helpDeskService.GetKnowledgeAsync(q, cancellationToken));
    }

    [HttpPost("tickets")]
    [ProducesResponseType(typeof(HelpDeskTicketResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<HelpDeskTicketResultDto>> CreateTicket(
        [FromBody] CreateHelpDeskTicketRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await helpDeskService.CreateTicketAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("areas")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskAreaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HelpDeskAreaDto>>> GetAreas(
        CancellationToken cancellationToken)
    {
        var areas = await helpDeskService.GetAreasAsync(cancellationToken);
        return Ok(areas);
    }

    [HttpGet("entities")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskGlpiEntityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HelpDeskGlpiEntityDto>>> GetEntities(
        CancellationToken cancellationToken)
    {
        var entities = await helpDeskService.GetEntitiesAsync(cancellationToken);
        return Ok(entities);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskItilCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HelpDeskItilCategoryDto>>> GetCategories(
        [FromQuery] string areaId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(areaId))
        {
            return BadRequest(new { detail = "Informe areaId válido." });
        }

        try
        {
            var categories = await helpDeskService.GetCategoriesAsync(areaId, cancellationToken);
            return Ok(categories);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { detail = exception.Message });
        }
    }

    [HttpGet("form-categories")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskFormCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HelpDeskFormCategoryDto>>> GetFormCategories(
        CancellationToken cancellationToken)
    {
        var categories = await helpDeskService.GetFormCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("forms")]
    [ProducesResponseType(typeof(IReadOnlyList<HelpDeskFormSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HelpDeskFormSummaryDto>>> GetForms(
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var forms = await helpDeskService.GetFormsAsync(categoryId, cancellationToken);
        return Ok(forms);
    }

    [HttpGet("forms/{formId:int}")]
    [ProducesResponseType(typeof(HelpDeskFormSchemaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HelpDeskFormSchemaDto>> GetFormSchema(
        int formId,
        CancellationToken cancellationToken)
    {
        var schema = await helpDeskService.GetFormSchemaAsync(formId, cancellationToken);
        return schema is null ? NotFound() : Ok(schema);
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

    [HttpGet("tickets/{ticketId}/attachments/{documentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketAttachment(
        string ticketId,
        string documentId,
        CancellationToken cancellationToken)
    {
        var file = await helpDeskService.GetTicketAttachmentAsync(ticketId, documentId, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpPost("tickets/{ticketId}/attachments")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UploadTicketAttachment(
        string ticketId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new { detail = "Selecione um arquivo válido." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await helpDeskService.UploadTicketAttachmentAsync(
                ticketId,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                stream,
                cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { detail = exception.Message });
        }
    }
}
