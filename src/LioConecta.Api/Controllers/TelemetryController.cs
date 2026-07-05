using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
[Authorize]
public sealed class TelemetryController(
    IObservabilityIngestionService ingestionService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("events")]
    [ProducesResponseType(typeof(TelemetryIngestResultDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TelemetryIngestResultDto>> IngestEvents(
        [FromBody] TelemetryEventsBatchDto batch,
        CancellationToken cancellationToken)
    {
        if (batch.Events.Count == 0)
        {
            return Accepted(new TelemetryIngestResultDto(0, 0));
        }

        Guid? userId = null;
        try
        {
            userId = await currentUserService.GetPersonIdAsync(cancellationToken);
        }
        catch
        {
            // Best effort — telemetria anônima parcial ainda correlacionável.
        }

        var result = await ingestionService.IngestEventsAsync(batch, userId, cancellationToken);
        return Accepted(result);
    }

    [HttpPost("page-views")]
    [ProducesResponseType(typeof(TelemetryIngestResultDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TelemetryIngestResultDto>> IngestPageViews(
        [FromBody] TelemetryPageViewsBatchDto batch,
        CancellationToken cancellationToken)
    {
        if (batch.Views.Count == 0)
        {
            return Accepted(new TelemetryIngestResultDto(0, 0));
        }

        Guid? userId = null;
        try
        {
            userId = await currentUserService.GetPersonIdAsync(cancellationToken);
        }
        catch
        {
            // Best effort.
        }

        var result = await ingestionService.IngestPageViewsAsync(batch, userId, cancellationToken);
        return Accepted(result);
    }
}
