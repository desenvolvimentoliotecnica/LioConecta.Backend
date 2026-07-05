using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public sealed class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResultDto>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var results = await searchService.SearchAsync(q, limit, cancellationToken);
        return Ok(results);
    }
}
