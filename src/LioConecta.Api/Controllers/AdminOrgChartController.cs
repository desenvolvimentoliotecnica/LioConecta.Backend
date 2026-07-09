using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/org-chart")]
[Authorize]
[RequirePermission("org_chart.govern")]
public sealed class AdminOrgChartController(
    IOrgChartGovernanceService governanceService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("settings")]
    [ProducesResponseType(typeof(OrgChartSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgChartSettingsDto>> GetSettings(CancellationToken cancellationToken)
        => Ok(await governanceService.GetSettingsAsync(cancellationToken));

    [HttpPut("settings")]
    [ProducesResponseType(typeof(OrgChartSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgChartSettingsDto>> SaveSettings(
        [FromBody] UpsertOrgChartSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await governanceService.SaveSettingsAsync(request, userId, cancellationToken));
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(OrgChartGovernanceSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgChartGovernanceSummaryDto>> GetSummary(CancellationToken cancellationToken)
        => Ok(await governanceService.GetSummaryAsync(cancellationToken));

    [HttpPost("import-from-graph")]
    [ProducesResponseType(typeof(OrgChartGovernanceSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgChartGovernanceSummaryDto>> ImportFromGraph(
        [FromBody] ImportFromGraphRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await governanceService.ImportFromGraphAsync(request, userId, cancellationToken));
    }

    [HttpGet("department-mappings")]
    [ProducesResponseType(typeof(IReadOnlyList<OrgDepartmentMappingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrgDepartmentMappingDto>>> ListDepartmentMappings(
        CancellationToken cancellationToken)
        => Ok(await governanceService.ListDepartmentMappingsAsync(cancellationToken));

    [HttpPatch("department-mappings/{id:guid}")]
    [ProducesResponseType(typeof(OrgDepartmentMappingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgDepartmentMappingDto>> UpdateDepartmentMapping(
        Guid id,
        [FromBody] UpsertOrgDepartmentMappingRequest request,
        CancellationToken cancellationToken)
        => Ok(await governanceService.UpdateDepartmentMappingAsync(id, request, cancellationToken));

    [HttpPost("import-departments-from-directory")]
    [ProducesResponseType(typeof(ImportDepartmentsFromDirectoryResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportDepartmentsFromDirectoryResultDto>> ImportDepartmentsFromDirectory(
        [FromBody] ImportDepartmentsFromDirectoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return Ok(await governanceService.ImportDepartmentsFromDirectoryAsync(request, userId, cancellationToken));
    }
}
