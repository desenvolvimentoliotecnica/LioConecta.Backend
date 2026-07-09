using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/app-settings")]
[Authorize]
[RequirePermission("admin.settings.manage")]
public sealed class AdminAppSettingsController(IAppSettingService appSettingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppSettingCategoryDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var categories = await appSettingService.GetGroupedAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AppSettingsUpdateResultDto>> BulkUpdate(
        [FromBody] BulkUpdateAppSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appSettingService.BulkUpdateAsync(request, cancellationToken);
        return Ok(result);
    }
}
