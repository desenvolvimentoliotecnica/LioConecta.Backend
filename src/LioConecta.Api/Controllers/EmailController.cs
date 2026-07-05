using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/email")]
[Authorize]
public sealed class EmailController(
    IEmailSendService emailSendService,
    IEmailAttachmentService emailAttachmentService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("attachments")]
    [ProducesResponseType(typeof(EmailAttachmentUploadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(12_582_912)]
    public async Task<ActionResult<EmailAttachmentUploadDto>> UploadAttachment(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Nenhum arquivo enviado." });
        }

        try
        {
            var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
            await using var stream = file.OpenReadStream();
            var result = await emailAttachmentService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                personId,
                cancellationToken);

            return Created($"/api/v1/email/attachments/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SendEmailResponse>> Send(
        [FromBody] SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
            var result = await emailSendService.SendAsync(request, personId, cancellationToken);
            return Accepted(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
