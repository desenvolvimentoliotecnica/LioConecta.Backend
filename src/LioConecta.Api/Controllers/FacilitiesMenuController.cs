using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/facilities/menu")]
[Authorize]
public sealed class FacilitiesMenuController(IFacilitiesMenuService menuService) : ControllerBase
{
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(MenuEditorBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MenuEditorBootstrapDto>> GetBootstrap(CancellationToken cancellationToken)
        => Ok(await menuService.GetBootstrapAsync(cancellationToken));

    [HttpGet("week")]
    [ProducesResponseType(typeof(WeeklyMenuDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WeeklyMenuDto>> GetWeek(
        [FromQuery] DateOnly start,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await menuService.GetWeeklyMenuAsync(start, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{date}")]
    [ProducesResponseType(typeof(DailyMenuDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DailyMenuDto>> SaveDailyMenu(
        DateOnly date,
        [FromBody] SaveDailyMenuRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        var saved = await menuService.SaveDailyMenuAsync(date, request, cancellationToken);
        return Ok(saved);
    }

    [HttpPost("{date}/copy-from")]
    [ProducesResponseType(typeof(DailyMenuDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyMenuDto>> CopyDailyMenu(
        DateOnly date,
        [FromBody] CopyMenuDayRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        try
        {
            var copied = await menuService.CopyDailyMenuAsync(date, request.SourceDate, cancellationToken);
            return Ok(copied);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("week/copy-from")]
    [ProducesResponseType(typeof(WeeklyMenuDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WeeklyMenuDto>> CopyWeeklyMenu(
        [FromBody] CopyMenuWeekRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        if (request.TargetWeekStart is null)
        {
            return BadRequest(new { message = "targetWeekStart é obrigatório." });
        }

        try
        {
            var copied = await menuService.CopyWeeklyMenuAsync(
                request.TargetWeekStart.Value,
                request.SourceWeekStart,
                cancellationToken);
            return Ok(copied);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("week/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadWeeklyPdf(
        [FromQuery] DateOnly start,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await menuService.GetWeeklyMenuPdfAsync(start, cancellationToken);
            var fileName = FacilitiesMenuPdfGenerator.BuildFileName(start);
            return File(bytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("week/send-email")]
    [ProducesResponseType(typeof(SendFacilitiesMenuEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SendFacilitiesMenuEmailResponse>> SendWeeklyEmail(
        [FromBody] SendFacilitiesMenuEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        try
        {
            var result = await menuService.SendWeeklyEmailAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDailyMenu(DateOnly date, CancellationToken cancellationToken)
    {
        if (!await CanEditAsync(cancellationToken))
        {
            return Forbid();
        }

        await menuService.DeleteDailyMenuAsync(date, cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanEditAsync(CancellationToken cancellationToken)
    {
        var policy = await menuService.GetEditorPolicyAsync(cancellationToken);
        return policy.CanEdit;
    }
}
