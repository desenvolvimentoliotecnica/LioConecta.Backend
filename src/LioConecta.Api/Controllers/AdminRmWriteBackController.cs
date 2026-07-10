using LioConecta.Api.Authorization;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

/// <summary>
/// Rollback administrativo do write-back SQL direto no TOTVS RM (Onda 1B).
/// Ver docs/spike-writeback-sql-rm.md.
/// </summary>
[ApiController]
[Route("api/v1/admin/rm-writeback")]
[Authorize]
[RequirePermission("admin.totvs.manage")]
public sealed class AdminRmWriteBackController(IRmWriteBackJournalService journalService) : ControllerBase
{
    [HttpPost("sessions/{sessionId:guid}/rollback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RollbackSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await journalService.RollbackSessionAsync(sessionId, cancellationToken);
        if (!result.Success && result.EntriesRolledBack == 0)
        {
            return NotFound(new { detail = result.Message });
        }

        return Ok(new
        {
            success = result.Success,
            entriesRolledBack = result.EntriesRolledBack,
            message = result.Message,
        });
    }
}
