using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/email")]
[Authorize]
[RequirePermission("admin.email.manage")]
public sealed class AdminEmailController(
    IEmailConfigurationService configurationService,
    IEmailAdminService emailAdminService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("config")]
    [ProducesResponseType(typeof(EmailConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailConfigurationDto>> GetConfig(CancellationToken cancellationToken)
    {
        return Ok(await configurationService.GetAsync(cancellationToken));
    }

    [HttpPut("config")]
    [ProducesResponseType(typeof(EmailConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailConfigurationDto>> SaveConfig(
        [FromBody] UpsertEmailConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await configurationService.SaveAsync(request, userId, cancellationToken));
    }

    [HttpPost("config/test")]
    [ProducesResponseType(typeof(EmailConnectionTestResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailConnectionTestResponse>> TestConfig(
        [FromBody] EmailSmtpTestRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await configurationService.TestConnectionAsync(request, cancellationToken));
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(EmailMessageSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailMessageSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await emailAdminService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("messages")]
    [ProducesResponseType(typeof(PagedEmailMessagesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedEmailMessagesDto>> ListMessages(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await emailAdminService.ListMessagesAsync(
            status,
            search,
            from,
            to,
            page,
            pageSize,
            cancellationToken));
    }

    [HttpGet("messages/{id:guid}")]
    [ProducesResponseType(typeof(EmailMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailMessageDto>> GetMessage(Guid id, CancellationToken cancellationToken)
    {
        var message = await emailAdminService.GetMessageAsync(id, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpPost("messages/{id:guid}/retry")]
    [ProducesResponseType(typeof(EmailMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailMessageDto>> RetryMessage(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var message = await emailAdminService.RetryMessageAsync(id, cancellationToken);
            return message is null ? NotFound() : Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("messages/{id:guid}/cancel")]
    [ProducesResponseType(typeof(EmailMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailMessageDto>> CancelMessage(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var message = await emailAdminService.CancelMessageAsync(id, cancellationToken);
            return message is null ? NotFound() : Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
