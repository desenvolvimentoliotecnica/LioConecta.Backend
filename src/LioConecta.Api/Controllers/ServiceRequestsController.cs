using LioConecta.Api.Authorization;
using LioConecta.Api.Configuration;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/service-requests")]
[Authorize]
public sealed class ServiceRequestsController(IServiceRequestService serviceRequestService) : ControllerBase
{
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestDto>>> GetMine(CancellationToken cancellationToken)
    {
        var requests = await serviceRequestService.GetMineAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpGet("types")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ServiceTypeDefinition>> GetTypes()
    {
        return Ok(ServiceTypesCatalog.All);
    }

    [HttpGet("management")]
    [RequirePermission("rh_requests.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestDto>>> GetManagement(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parsedStatus = ParseStatus(status);
            var items = await serviceRequestService.GetManagementListAsync(
                parsedStatus,
                q,
                limit,
                cancellationToken);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("management/{id:guid}")]
    [RequirePermission("rh_requests.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServiceRequestDto>> GetManagementById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await serviceRequestService.GetManagementDetailAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("management/{id:guid}/approve")]
    [RequirePermission("rh_requests.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> Approve(
        Guid id,
        [FromBody] ApproveServiceRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await serviceRequestService.ApproveAsync(
                id,
                request ?? new ApproveServiceRequestDto(null),
                cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("management/{id:guid}/reject")]
    [RequirePermission("rh_requests.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> Reject(
        Guid id,
        [FromBody] RejectServiceRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await serviceRequestService.RejectAsync(
                id,
                request ?? new RejectServiceRequestDto(null),
                cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("management/{id:guid}/messages")]
    [RequirePermission("rh_requests.manage")]
    [RequestSizeLimit(ServiceRequestAttachmentLimits.MaxFileSizeBytes * ServiceRequestAttachmentLimits.MaxFilesPerMessage + 1_048_576)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> ReplyAsManager(
        Guid id,
        [FromForm] string? message,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        var attachments = new List<ServiceRequestAttachmentInput>();
        try
        {
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
                attachments.Add(new ServiceRequestAttachmentInput(
                    buffered,
                    file.FileName,
                    file.ContentType,
                    buffered.Length));
            }

            var item = await serviceRequestService.ReplyAsManagerAsync(
                id,
                message,
                attachments,
                cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            foreach (var attachment in attachments)
            {
                await attachment.Content.DisposeAsync();
            }
        }
    }

    [HttpPost("management/{id:guid}/finalize")]
    [RequirePermission("rh_requests.manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> Finalize(
        Guid id,
        [FromBody] FinalizeServiceRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await serviceRequestService.FinalizeAsync(
                id,
                request ?? new FinalizeServiceRequestDto(null),
                cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/attachments/{storageFileName}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttachment(
        Guid id,
        string storageFileName,
        CancellationToken cancellationToken)
    {
        var file = await serviceRequestService.GetAttachmentAsync(id, storageFileName, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("{id:guid}/messages")]
    [RequestSizeLimit(ServiceRequestAttachmentLimits.MaxFileSizeBytes * ServiceRequestAttachmentLimits.MaxFilesPerMessage + 1_048_576)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> ReplyAsRequester(
        Guid id,
        [FromForm] string? message,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        var attachments = new List<ServiceRequestAttachmentInput>();
        try
        {
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
                attachments.Add(new ServiceRequestAttachmentInput(
                    buffered,
                    file.FileName,
                    file.ContentType,
                    buffered.Length));
            }

            var item = await serviceRequestService.ReplyAsRequesterAsync(
                id,
                message,
                attachments,
                cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            foreach (var attachment in attachments)
            {
                await attachment.Content.DisposeAsync();
            }
        }
    }

    [HttpPost("{id:guid}/confirm-closure")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> ConfirmClosure(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await serviceRequestService.ConfirmClosureAsync(id, cancellationToken);
            return item is null ? NotFound() : Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await serviceRequestService.GetByIdAsync(id, cancellationToken);
        return request is null ? NotFound() : Ok(request);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceRequestDto>> Create(
        [FromBody] CreateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var created = await serviceRequestService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    private static ServiceRequestStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse<ServiceRequestStatus>(status.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
