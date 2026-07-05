using LioConecta.Api.Authorization;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

/// <summary>
/// Endpoints restritos para totens/quiosque — somente leitura de feed e comunicados.
/// </summary>
[ApiController]
[Route("api/v1/kiosk")]
[Authorize(Policy = AuthPolicies.RequireKioskReader)]
public sealed class KioskController(
    IFeedService feedService,
    IComunicadoService comunicadoService) : ControllerBase
{
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await feedService.GetFeedAsync(
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("comunicados/{id:guid}")]
    public async Task<IActionResult> GetComunicado(Guid id, CancellationToken cancellationToken)
    {
        var item = await comunicadoService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("comunicados")]
    public async Task<IActionResult> ListComunicados(
        [FromQuery] ComunicadoKind? kind,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await comunicadoService.ListAsync(
            kind,
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);
        return Ok(result);
    }
}
