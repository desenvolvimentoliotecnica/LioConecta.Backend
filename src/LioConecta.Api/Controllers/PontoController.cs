using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/rh/ponto")]
[Authorize]
public sealed class PontoController(
    IPontoService pontoService,
    IPontoAdjustmentService pontoAdjustmentService,
    IHourBankService hourBankService) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [HttpGet("periods")]
    [ProducesResponseType(typeof(PontoPeriodSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PontoPeriodSettingsDto>> GetPeriods(CancellationToken cancellationToken)
    {
        var response = await pontoService.GetPeriodSettingsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PontoResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PontoResponseDto>> Get(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var response = await pontoService.GetTimesheetAsync(month, year, cancellationToken);
        return Ok(response);
    }

    [HttpGet("banco-horas")]
    [ProducesResponseType(typeof(IReadOnlyList<HourBankTeamMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<HourBankTeamMemberDto>>> GetTeamBancoHoras(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await hourBankService.GetTeamAsync(q, cancellationToken);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("banco-horas/{personId:guid}")]
    [ProducesResponseType(typeof(LeaveBancoHorasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LeaveBancoHorasDto>> GetPersonBancoHoras(
        Guid personId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await hourBankService.GetForPersonAsync(personId, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("adjustments")]
    [ProducesResponseType(typeof(IReadOnlyList<PontoAdjustmentItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PontoAdjustmentItemDto>>> GetMyAdjustments(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var items = await pontoAdjustmentService.GetMineAsync(limit, cancellationToken);
        return Ok(items);
    }

    [HttpGet("adjustments/{id:guid}")]
    [ProducesResponseType(typeof(PontoAdjustmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PontoAdjustmentDetailDto>> GetMyAdjustmentDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await pontoAdjustmentService.GetMineDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("adjustments/management")]
    [ProducesResponseType(typeof(IReadOnlyList<PontoAdjustmentManagementItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PontoAdjustmentManagementItemDto>>> GetManagement(
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await pontoAdjustmentService.GetManagementListAsync(status, q, limit, cancellationToken);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("adjustments/management/{id:guid}")]
    [ProducesResponseType(typeof(PontoAdjustmentManagementDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PontoAdjustmentManagementDetailDto>> GetManagementDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await pontoAdjustmentService.GetManagementDetailAsync(id, cancellationToken);
            return detail is null ? NotFound() : Ok(detail);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("adjustments/management/{id:guid}/attachments/{storageFileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManagementAttachment(
        Guid id,
        string storageFileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await pontoAdjustmentService.GetManagementAttachmentAsync(
                id,
                storageFileName,
                cancellationToken);
            if (file is null)
            {
                return NotFound();
            }

            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("adjustments")]
    [ProducesResponseType(typeof(PontoAdjustmentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PontoAdjustmentResultDto>> CreateAdjustment(
        [FromBody] CreatePontoAdjustmentDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await pontoAdjustmentService.CreateAsync(request, null, cancellationToken);
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

    [HttpPost("adjustments/multipart")]
    [RequestSizeLimit(PontoAttachmentStore.MaxFileSizeBytes * PontoAttachmentStore.MaxFilesPerRequest + 1_048_576)]
    [ProducesResponseType(typeof(PontoAdjustmentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PontoAdjustmentResultDto>> CreateAdjustmentMultipart(
        [FromForm] string payload,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest(new { detail = "Informe o payload da solicitação." });
        }

        CreatePontoAdjustmentDto? request;
        try
        {
            request = JsonSerializer.Deserialize<CreatePontoAdjustmentDto>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest(new { detail = "Payload inválido." });
        }

        if (request is null)
        {
            return BadRequest(new { detail = "Payload inválido." });
        }

        var attachments = new List<PontoAttachmentInput>();
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
                attachments.Add(new PontoAttachmentInput(
                    buffered,
                    file.FileName,
                    file.ContentType,
                    buffered.Length));
            }

            var result = await pontoAdjustmentService.CreateAsync(request, attachments, cancellationToken);
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
