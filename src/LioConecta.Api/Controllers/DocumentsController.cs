using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(
        [FromQuery] string? category,
        CancellationToken cancellationToken = default)
    {
        var documents = await documentService.ListAsync(category, cancellationToken);
        return Ok(documents);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var document = await documentService.GetByIdAsync(id, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }
}
