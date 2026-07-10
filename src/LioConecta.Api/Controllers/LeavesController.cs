using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Services;
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

    [HttpGet("requests/{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequestPdf(Guid id, CancellationToken cancellationToken)
    {
        var bytes = await leaveService.GetRequestPdfAsync(id, cancellationToken);
        return bytes is null
            ? NotFound()
            : File(bytes, "application/pdf", $"comprovante-ferias-{id:N}.pdf");
    }

    [HttpGet("management")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveManagementItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<LeaveManagementItemDto>>> GetManagement(
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await leaveService.GetManagementListAsync(status, q, limit, cancellationToken);
        return Ok(items);
    }

    [HttpGet("management/{id:guid}")]
    [ProducesResponseType(typeof(LeaveManagementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LeaveManagementDetailDto>> GetManagementDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await leaveService.GetManagementDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("management/{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetManagementPdf(Guid id, CancellationToken cancellationToken)
    {
        var bytes = await leaveService.GetManagementPdfAsync(id, cancellationToken);
        return bytes is null
            ? NotFound()
            : File(bytes, "application/pdf", $"comprovante-ferias-gestao-{id:N}.pdf");
    }

    [HttpGet("management/{id:guid}/attachments/{storageFileName}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetManagementAttachment(
        Guid id,
        string storageFileName,
        CancellationToken cancellationToken)
    {
        var file = await leaveService.GetManagementAttachmentAsync(id, storageFileName, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeaveRequestResultDto>> CreateRequest(
        [FromBody] CreateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await leaveService.CreateRequestAsync(request, null, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [HttpPost("requests/multipart")]
    [RequestSizeLimit(LeaveAttachmentStore.MaxFileSizeBytes * LeaveAttachmentStore.MaxFilesPerRequest + 1_048_576)]
    [ProducesResponseType(typeof(LeaveRequestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeaveRequestResultDto>> CreateRequestMultipart(
        [FromForm] string serviceId,
        [FromForm] DateOnly? startDate,
        [FromForm] DateOnly? endDate,
        [FromForm] int? days,
        [FromForm] string? notes,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            return BadRequest(new { detail = "Informe o serviço." });
        }

        var request = new CreateLeaveRequestDto(serviceId.Trim(), startDate, endDate, days, notes);
        var attachments = new List<LeaveAttachmentInput>();

        try
        {
            // Buffer each multipart section immediately. Holding multiple IFormFile
            // OpenReadStream() handles causes "inner stream position has changed unexpectedly".
            foreach (var file in files ?? [])
            {
                if (file is null || file.Length <= 0)
                {
                    continue;
                }

                var buffered = new MemoryStream();
                await using (var upload = file.OpenReadStream())
                {
                    await upload.CopyToAsync(buffered, cancellationToken);
                }

                buffered.Position = 0;
                attachments.Add(new LeaveAttachmentInput(
                    buffered,
                    file.FileName,
                    file.ContentType,
                    buffered.Length));
            }

            var result = await leaveService.CreateRequestAsync(request, attachments, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        finally
        {
            foreach (var attachment in attachments)
            {
                await attachment.Content.DisposeAsync();
            }
        }
    }
}
