using LioConecta.Api.Configuration;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/service-requests")]
[Authorize]
public sealed class ServiceRequestsController(IServiceRequestService serviceRequestService) : ControllerBase
{
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServiceRequestDto>>> GetMine(CancellationToken cancellationToken)
    {
        var requests = await serviceRequestService.GetMineAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpGet("types")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ServiceTypeDefinition>> GetTypes()
    {
        return Ok(ServiceTypesCatalog.All);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await serviceRequestService.GetByIdAsync(id, cancellationToken);
        return request is null ? NotFound() : Ok(request);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceRequestDto>> Create(
        [FromBody] CreateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var created = await serviceRequestService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
