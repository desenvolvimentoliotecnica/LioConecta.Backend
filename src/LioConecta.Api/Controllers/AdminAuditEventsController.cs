using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/audit-events")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public sealed class AdminAuditEventsController(IAuditService auditService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditEventSummaryDto>> GetSummary(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var result = await auditService.GetSummaryAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [HttpGet("actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetActions(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await auditService.GetDistinctActionsAsync(from, to, limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("target-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTargetTypes(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await auditService.GetDistinctTargetTypesAsync(from, to, limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedAuditEventsDto>> Get(
        [FromQuery] string? action,
        [FromQuery] Guid? actorId,
        [FromQuery] string? targetType,
        [FromQuery] Guid? correlationId,
        [FromQuery] AuditSource? source,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? httpStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await auditService.QueryAsync(
            new AuditEventQuery(action, actorId, targetType, correlationId, source, from, to, httpStatus, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
