using LioConecta.Api.Attributes;
using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/admin/observability")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
[AccessAudited(Resource = "ObservabilityHub")]
public sealed class AdminObservabilityController(IObservabilityQueryService queryService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ObservabilitySummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ObservabilitySummaryDto>> GetSummary(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.GetSummaryAsync(from, to, cancellationToken);
        return Ok(result);
    }

    [HttpGet("errors")]
    [ProducesResponseType(typeof(PagedObservabilityEventsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedObservabilityEventsDto>> GetErrors(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? eventName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.QueryErrorsAsync(
            new ObservabilityEventQuery(from, to, eventName, MinSeverity: 4, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("page-views")]
    [ProducesResponseType(typeof(PagedPageViewsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedPageViewsDto>> GetPageViews(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? module,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.QueryPageViewsAsync(
            new PageViewQuery(from, to, module, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("access-events")]
    [ProducesResponseType(typeof(PagedAccessEventsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedAccessEventsDto>> GetAccessEvents(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? result,
        [FromQuery] string? eventName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var queryResult = await queryService.QueryAccessEventsAsync(
            new AccessEventQuery(from, to, result, eventName, page, pageSize),
            cancellationToken);

        return Ok(queryResult);
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ObservabilityMetricsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ObservabilityMetricsDto>> GetMetrics(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? period,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.GetMetricsAsync(from, to, period, cancellationToken);
        return Ok(result);
    }

    [HttpGet("investigate")]
    [ProducesResponseType(typeof(ObservabilityTimelineDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ObservabilityTimelineDto>> Investigate(
        [FromQuery] Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        if (correlationId == Guid.Empty)
        {
            return BadRequest(new { message = "correlationId is required." });
        }

        var result = await queryService.InvestigateAsync(correlationId, cancellationToken);
        return Ok(result);
    }
}
