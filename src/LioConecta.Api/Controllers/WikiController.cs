using LioConecta.Api.Authorization;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/wiki")]
[Authorize]
public sealed class WikiController(IWikiService wikiService) : ControllerBase
{
    [HttpGet("articles")]
    [ProducesResponseType(typeof(IReadOnlyList<WikiArticleListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WikiArticleListItemDto>>> List(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] WikiArticleStatus? status,
        [FromQuery] bool manage = false,
        CancellationToken cancellationToken = default)
    {
        var articles = await wikiService.ListAsync(q, category, status, manage, cancellationToken);
        return Ok(articles);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<WikiCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WikiCategoryDto>>> GetCategories(
        [FromQuery] bool manage = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await wikiService.GetCategoriesAsync(manage, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("articles/{slug}")]
    [ProducesResponseType(typeof(WikiArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WikiArticleDto>> GetBySlug(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var article = await wikiService.GetBySlugAsync(slug, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost("articles")]
    [RequirePermission("wiki.manage")]
    [ProducesResponseType(typeof(WikiArticleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WikiArticleDto>> Create(
        [FromBody] CreateWikiArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        var article = await wikiService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = article.Slug }, article);
    }

    [HttpPatch("articles/{id:guid}")]
    [RequirePermission("wiki.manage")]
    [ProducesResponseType(typeof(WikiArticleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WikiArticleDto>> Update(
        Guid id,
        [FromBody] UpdateWikiArticleRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await wikiService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("articles/{id:guid}/publish")]
    [RequirePermission("wiki.manage")]
    [ProducesResponseType(typeof(WikiArticleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WikiArticleDto>> Publish(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await wikiService.PublishAsync(id, cancellationToken));

    [HttpPost("articles/{id:guid}/archive")]
    [RequirePermission("wiki.manage")]
    [ProducesResponseType(typeof(WikiArticleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WikiArticleDto>> Archive(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Ok(await wikiService.ArchiveAsync(id, cancellationToken));
}
