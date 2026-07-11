using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Api.Authorization;
using LioConecta.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/comunicados")]
[Authorize]
public sealed class ComunicadosController(
    IComunicadoService comunicadoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] ComunicadoKind? kind,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        [FromQuery] bool archived = false,
        [FromQuery] bool manage = false,
        CancellationToken cancellationToken = default)
    {
        var page = await comunicadoService.ListAsync(
            kind,
            archived,
            manage,
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);

        return Ok(page);
    }

    [HttpGet("hub")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ComunicadoHubDto>> GetHub(CancellationToken cancellationToken)
    {
        var hub = await comunicadoService.GetHubAsync(cancellationToken);
        return Ok(hub);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var comunicado = await comunicadoService.GetByIdAsync(id, cancellationToken);
        return comunicado is null ? NotFound() : Ok(comunicado);
    }

    [HttpGet("slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var comunicado = await comunicadoService.GetBySlugAsync(slug, cancellationToken);
        return comunicado is null ? NotFound() : Ok(comunicado);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await comunicadoService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateComunicadoRequest request,
        CancellationToken cancellationToken)
    {
        var comunicado = await comunicadoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = comunicado.Id }, comunicado);
    }

    [HttpPatch("{id:guid}")]
    [RequirePermission("comunicados.manage")]
    public async Task<ActionResult<ComunicadoDto>> Update(
        Guid id,
        [FromBody] UpdateComunicadoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await comunicadoService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/publish")]
    [RequirePermission("comunicados.manage")]
    public async Task<ActionResult<ComunicadoDto>> Publish(Guid id, CancellationToken cancellationToken) =>
        Ok(await comunicadoService.PublishAsync(id, cancellationToken));

    [HttpPost("{id:guid}/archive")]
    [RequirePermission("comunicados.manage")]
    public async Task<ActionResult<ComunicadoDto>> Archive(Guid id, CancellationToken cancellationToken) =>
        Ok(await comunicadoService.ArchiveAsync(id, cancellationToken));

    [HttpPost("{id:guid}/schedule")]
    [RequirePermission("comunicados.manage")]
    public async Task<ActionResult<ComunicadoDto>> Schedule(
        Guid id,
        [FromBody] ScheduleComunicadoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await comunicadoService.ScheduleAsync(id, request.ScheduledAt, cancellationToken));

    [HttpGet("{id:guid}/metrics")]
    [RequirePermission("comunicados.manage")]
    public async Task<ActionResult<ComunicadoMetricsDto>> GetMetrics(Guid id, CancellationToken cancellationToken) =>
        Ok(await comunicadoService.GetMetricsAsync(id, cancellationToken));
}
