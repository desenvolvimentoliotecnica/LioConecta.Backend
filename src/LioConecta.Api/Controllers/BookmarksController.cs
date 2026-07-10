using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/bookmarks")]
[Authorize]
public sealed class BookmarksController(IBookmarkCatalogService bookmarkCatalogService) : ControllerBase
{
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<BookmarkCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookmarkCatalogItemDto>>> GetCatalog(
        CancellationToken cancellationToken = default)
        => Ok(await bookmarkCatalogService.ListCatalogAsync(cancellationToken));
}
