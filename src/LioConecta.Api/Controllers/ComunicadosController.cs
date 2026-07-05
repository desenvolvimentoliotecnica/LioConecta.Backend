using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Api.Authorization;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/comunicados")]
[Authorize]
public sealed class ComunicadosController(
    IComunicadoService comunicadoService,
    INotificationService notificationService,
    AppDbContext dbContext,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] ComunicadoKind? kind,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        [FromQuery] bool archived = false,
        CancellationToken cancellationToken = default)
    {
        var page = await comunicadoService.ListAsync(
            kind,
            archived,
            new CursorPageRequest { Cursor = cursor, Limit = limit },
            cancellationToken);

        return Ok(page);
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
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateComunicadoRequest request,
        CancellationToken cancellationToken)
    {
        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var comunicadoId = Guid.NewGuid();

        var comunicado = new Comunicado
        {
            Id = comunicadoId,
            Kind = request.Kind,
            Title = request.Title.Trim(),
            Slug = SlugHelper.FromTitle(request.Title, comunicadoId),
            Excerpt = request.Excerpt?.Trim(),
            ContentJson = JsonSerializer.Serialize(request.Content ?? new Dictionary<string, object?>()),
            AuthorId = authorId,
            HeroImageUrl = request.HeroImageUrl,
            IsMandatory = request.IsMandatory,
            PublishedAt = request.PublishedAt ?? now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Comunicados.Add(comunicado);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(comunicado).Reference(c => c.Author).LoadAsync(cancellationToken);

        await notificationService.NotifyComunicadoCreatedAsync(comunicado, cancellationToken);

        var dto = ComunicadoMapper.ToDto(comunicado, isRead: false);
        return CreatedAtAction(nameof(GetById), new { id = comunicado.Id }, dto);
    }
}

public sealed record CreateComunicadoRequest(
    ComunicadoKind Kind,
    string Title,
    string? Excerpt,
    IReadOnlyDictionary<string, object?>? Content,
    string? HeroImageUrl,
    bool IsMandatory,
    DateTimeOffset? PublishedAt);
