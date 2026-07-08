using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/ramais")]
[Authorize]
public sealed class PhoneExtensionsController(IPhoneExtensionService phoneExtensionService) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(PhoneExtensionsBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PhoneExtensionsBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
        => Ok(await phoneExtensionService.GetBootstrapAsync(cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PhoneExtensionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PhoneExtensionDto>>> List(
        [FromQuery] string? q, [FromQuery] string? department, [FromQuery] bool? personLinked,
        [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
        => Ok(await phoneExtensionService.ListAsync(q, department, personLinked, includeInactive, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PhoneExtensionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhoneExtensionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await phoneExtensionService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PhoneExtensionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PhoneExtensionDto>> Create([FromBody] UpsertPhoneExtensionRequest request, CancellationToken cancellationToken)
    {
        var policy = await phoneExtensionService.GetManagePolicyAsync(cancellationToken);
        if (!policy.CanManage) return Forbid();
        try
        {
            var created = await phoneExtensionService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PhoneExtensionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PhoneExtensionDto>> Update(Guid id, [FromBody] UpsertPhoneExtensionRequest request, CancellationToken cancellationToken)
    {
        var policy = await phoneExtensionService.GetManagePolicyAsync(cancellationToken);
        if (!policy.CanManage) return Forbid();
        try { return Ok(await phoneExtensionService.UpdateAsync(id, request, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var policy = await phoneExtensionService.GetManagePolicyAsync(cancellationToken);
        if (!policy.CanManage) return Forbid();
        try
        {
            await phoneExtensionService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}