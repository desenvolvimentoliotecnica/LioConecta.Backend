using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/org-chart")]
[Authorize]
public sealed class OrgChartController(IOrgChartGovernanceService governanceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GovernedOrgChartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GovernedOrgChartDto>> GetChart(CancellationToken cancellationToken)
        => Ok(await governanceService.GetChartAsync(cancellationToken));

    [HttpGet("policy")]
    [ProducesResponseType(typeof(OrgChartPolicyDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgChartPolicyDto>> GetPolicy(CancellationToken cancellationToken)
        => Ok(await governanceService.GetPolicyAsync(cancellationToken));

    [HttpGet("departments")]
    [ProducesResponseType(typeof(IReadOnlyList<OrgDepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrgDepartmentDto>>> ListDepartments(CancellationToken cancellationToken)
        => Ok(await governanceService.ListDepartmentsAsync(cancellationToken));

    [HttpPost("departments")]
    [ProducesResponseType(typeof(OrgDepartmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrgDepartmentDto>> CreateDepartment(
        [FromBody] UpsertOrgDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        var department = await governanceService.CreateDepartmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ListDepartments), new { id = department.Id }, department);
    }

    [HttpPatch("departments/{id:guid}")]
    [ProducesResponseType(typeof(OrgDepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrgDepartmentDto>> UpdateDepartment(
        Guid id,
        [FromBody] UpsertOrgDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        try
        {
            return Ok(await governanceService.UpdateDepartmentAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("departments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        try
        {
            await governanceService.DeleteDepartmentAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("positions")]
    [ProducesResponseType(typeof(IReadOnlyList<OrgPositionDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrgPositionDetailDto>>> ListPositions(CancellationToken cancellationToken)
        => Ok(await governanceService.ListPositionsAsync(cancellationToken));

    [HttpPatch("positions/{id:guid}")]
    [ProducesResponseType(typeof(OrgPositionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrgPositionDetailDto>> UpdatePosition(
        Guid id,
        [FromBody] UpsertOrgPositionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        try
        {
            return Ok(await governanceService.UpdatePositionAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("positions")]
    [ProducesResponseType(typeof(OrgPositionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrgPositionDetailDto>> CreatePosition(
        [FromBody] CreateOrgPositionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        try
        {
            var position = await governanceService.CreatePositionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(ListPositions), new { id = position.Id }, position);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("positions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePosition(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        try
        {
            await governanceService.DeletePositionAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private async Task<bool> CanEditAsync(CancellationToken cancellationToken)
    {
        var policy = await governanceService.GetPolicyAsync(cancellationToken);
        return policy.CanEdit;
    }
}
