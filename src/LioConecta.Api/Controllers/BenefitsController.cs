using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/rh/benefits")]
[Authorize]
public sealed class BenefitsController(
    IBenefitService benefitService,
    IBenefitCatalogService catalogService,
    IBenefitManagementService managementService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(BenefitSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BenefitSummaryDto>> GetSummary(CancellationToken cancellationToken)
        => Ok(await benefitService.GetSummaryAsync(cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BenefitListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BenefitListItemDto>>> List(CancellationToken cancellationToken)
        => Ok(await benefitService.ListAsync(cancellationToken));

    [HttpPost("requests")]
    [ProducesResponseType(typeof(BenefitRequestResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BenefitRequestResultDto>> CreateRequest(
        [FromBody] CreateBenefitRequestDto request,
        CancellationToken cancellationToken)
        => Ok(await benefitService.CreateRequestAsync(request, cancellationToken));

    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(BenefitsBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BenefitsBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
        => Ok(await managementService.GetBootstrapAsync(cancellationToken));

    [HttpGet("catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<BenefitCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BenefitCatalogItemDto>>> ListCatalog(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok(await catalogService.ListAsync(q, category, includeInactive, cancellationToken));

    [HttpGet("catalog/{id:guid}")]
    [ProducesResponseType(typeof(BenefitCatalogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitCatalogItemDto>> GetCatalogById(Guid id, CancellationToken cancellationToken)
    {
        var item = await catalogService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("catalog")]
    [ProducesResponseType(typeof(BenefitCatalogItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BenefitCatalogItemDto>> CreateCatalog(
        [FromBody] UpsertBenefitCatalogRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await catalogService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetCatalogById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("catalog/{id:guid}")]
    [ProducesResponseType(typeof(BenefitCatalogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BenefitCatalogItemDto>> UpdateCatalog(
        Guid id,
        [FromBody] UpsertBenefitCatalogRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await catalogService.UpdateAsync(id, request, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("catalog/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCatalog(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await catalogService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("management")]
    [ProducesResponseType(typeof(IReadOnlyList<BenefitManagementListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BenefitManagementListItemDto>>> ListManagement(
        [FromQuery] Guid? personId,
        [FromQuery] string? departmentId,
        [FromQuery] string? catalogKey,
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await managementService.ListManagementAsync(
                personId, departmentId, catalogKey, q, category, includeInactive, cancellationToken));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("management/bulk-preview")]
    [ProducesResponseType(typeof(BulkBenefitPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkBenefitPreviewDto>> BulkPreview(
        [FromQuery] string operation,
        [FromQuery] string? catalogKey,
        [FromQuery] string? onDuplicate,
        [FromQuery] Guid[]? personIds,
        [FromQuery] string[]? departmentIds,
        [FromQuery] Guid[]? excludePersonIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var target = new BulkBenefitTargetRequest(personIds, departmentIds, excludePersonIds);
            return Ok(await managementService.BulkPreviewAsync(
                operation, target, catalogKey, onDuplicate, cancellationToken));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("management/{id:guid}")]
    [ProducesResponseType(typeof(BenefitEmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitEmployeeDetailDto>> GetManagementDetail(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await managementService.GetManagementDetailAsync(id, cancellationToken);
            return detail is null ? NotFound() : Ok(detail);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("management")]
    [ProducesResponseType(typeof(BenefitEmployeeDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BenefitEmployeeDetailDto>> CreateManagement(
        [FromBody] UpsertEmployeeBenefitRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await managementService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetManagementDetail), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("management/{id:guid}")]
    [ProducesResponseType(typeof(BenefitEmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BenefitEmployeeDetailDto>> UpdateManagement(
        Guid id,
        [FromBody] UpsertEmployeeBenefitRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await managementService.UpdateAsync(id, request, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("management/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteManagement(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await managementService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("management/from-catalog")]
    [ProducesResponseType(typeof(BenefitEmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BenefitEmployeeDetailDto>> AssignFromCatalog(
        [FromBody] AssignBenefitFromCatalogRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await managementService.AssignFromCatalogAsync(request, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("management/bulk-from-catalog")]
    [ProducesResponseType(typeof(BulkBenefitOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkBenefitOperationResultDto>> BulkAssignFromCatalog(
        [FromBody] BulkAssignBenefitsRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await managementService.BulkAssignFromCatalogAsync(request, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("management/bulk-set-active")]
    [ProducesResponseType(typeof(BulkBenefitOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkBenefitOperationResultDto>> BulkSetActive(
        [FromBody] BulkSetActiveBenefitsRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await managementService.BulkSetActiveAsync(request, cancellationToken)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{benefitId}")]
    [ProducesResponseType(typeof(BenefitDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitDetailDto>> GetDetail(string benefitId, CancellationToken cancellationToken)
    {
        var detail = await benefitService.GetDetailAsync(benefitId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }
}
