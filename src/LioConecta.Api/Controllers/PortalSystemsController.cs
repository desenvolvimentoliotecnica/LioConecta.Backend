using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/systems")]
[Authorize]
public sealed class PortalSystemsController(ISystemCatalogService systemCatalogService) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(SystemsBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemsBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
        => Ok(await systemCatalogService.GetBootstrapAsync(cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PortalSystemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PortalSystemDto>>> List(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok(await systemCatalogService.ListAsync(q, category, includeInactive, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PortalSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortalSystemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await systemCatalogService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PortalSystemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortalSystemDto>> Create(
        [FromBody] UpsertPortalSystemRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await systemCatalogService.GetManagePolicyAsync(cancellationToken);
        if (!policy.CanManage)
        {
            return Forbid();
        }

        try
        {
            var created = await systemCatalogService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PortalSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortalSystemDto>> Update(
        Guid id,
        [FromBody] UpsertPortalSystemRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await systemCatalogService.GetManagePolicyAsync(cancellationToken);
        if (!policy.CanManage)
        {
            return Forbid();
        }

        try
        {
            return Ok(await systemCatalogService.UpdateAsync(id, request, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var policy = await systemCatalogService.GetManagePolicyAsync(cancellationToken);
        if (!policy.CanManage)
        {
            return Forbid();
        }

        try
        {
            await systemCatalogService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/click")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordClick(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await systemCatalogService.RecordClickAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/icon")]
    [ProducesResponseType(typeof(UploadSystemIconResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(2_097_152)]
    public async Task<ActionResult<UploadSystemIconResponseDto>> UploadIcon(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var policy = await systemCatalogService.GetManagePolicyAsync(cancellationToken);
        if (!policy.CanManage)
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Nenhum arquivo enviado." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await systemCatalogService.UploadIconAsync(
                id,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                cancellationToken);

            return Created(result.Url, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
