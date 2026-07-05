using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/rh/benefits")]
[Authorize]
public sealed class BenefitsController(IBenefitService benefitService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(BenefitSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BenefitSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await benefitService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BenefitListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BenefitListItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await benefitService.ListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{benefitId}")]
    [ProducesResponseType(typeof(BenefitDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BenefitDetailDto>> GetDetail(
        string benefitId,
        CancellationToken cancellationToken)
    {
        var detail = await benefitService.GetDetailAsync(benefitId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("requests")]
    [ProducesResponseType(typeof(BenefitRequestResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BenefitRequestResultDto>> CreateRequest(
        [FromBody] CreateBenefitRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await benefitService.CreateRequestAsync(request, cancellationToken);
        return Ok(result);
    }
}
