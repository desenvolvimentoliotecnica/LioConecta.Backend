using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LioConecta.Api.Controllers;

[ApiController]
[Route("api/v1/rh/payslips")]
[Authorize]
public sealed class PayslipsController(IPayslipService payslipService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PayslipSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PayslipSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await payslipService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("services")]
    [ProducesResponseType(typeof(IReadOnlyList<PayslipServiceDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PayslipServiceDto>> GetServices()
    {
        return Ok(payslipService.GetServices());
    }

    [HttpGet("comparativo")]
    [ProducesResponseType(typeof(PayslipComparativoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayslipComparativoDto>> GetComparativo(
        [FromQuery] int fromYear,
        [FromQuery] int fromMonth,
        [FromQuery] int toYear,
        [FromQuery] int toMonth,
        CancellationToken cancellationToken)
    {
        var result = await payslipService.GetComparativoAsync(
            fromYear,
            fromMonth,
            toYear,
            toMonth,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("consultas/fgts")]
    [ProducesResponseType(typeof(FgtsConsultaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FgtsConsultaDto>> GetFgts(CancellationToken cancellationToken)
    {
        return Ok(await payslipService.GetFgtsConsultaAsync(cancellationToken));
    }

    [HttpGet("consultas/descontos")]
    [ProducesResponseType(typeof(DescontosConsultaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DescontosConsultaDto>> GetDescontos(CancellationToken cancellationToken)
    {
        return Ok(await payslipService.GetDescontosConsultaAsync(cancellationToken));
    }

    [HttpGet("consultas/rubricas")]
    [ProducesResponseType(typeof(RubricasConsultaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RubricasConsultaDto>> GetRubricas(CancellationToken cancellationToken)
    {
        return Ok(await payslipService.GetRubricasConsultaAsync(cancellationToken));
    }

    [HttpGet("informe/{year:int}")]
    [ProducesResponseType(typeof(IncomeStatementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncomeStatementDto>> GetInforme(int year, CancellationToken cancellationToken)
    {
        var informe = await payslipService.GetIncomeStatementAsync(year, cancellationToken);
        return informe is null ? NotFound() : Ok(informe);
    }

    [HttpPost("requests")]
    [ProducesResponseType(typeof(PayslipRequestResultDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PayslipRequestResultDto>> CreateRequest(
        [FromBody] CreatePayslipRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await payslipService.CreateRequestAsync(request, cancellationToken);
        return CreatedAtAction(nameof(CreateRequest), result);
    }

    [HttpGet("comprovante/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComprovantePdf(CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await payslipService.GetComprovantePdfAsync(cancellationToken);
            return File(bytes, "application/pdf", "comprovante-rendimentos.pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("carta-consignacao/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartaConsignacaoPdf(CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await payslipService.GetCartaConsignacaoPdfAsync(cancellationToken);
            return File(bytes, "application/pdf", "carta-consignacao.pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("{year:int}/{month:int}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(
        int year,
        int month,
        [FromQuery] string? paymentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await payslipService.GetPdfAsync(year, month, paymentType, cancellationToken);
            return File(bytes, "application/pdf", $"contracheque-{year}-{month:D2}.pdf");
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(PayslipDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayslipDetailDto>> GetDetail(
        int year,
        int month,
        [FromQuery] string? paymentType,
        CancellationToken cancellationToken)
    {
        var detail = await payslipService.GetDetailAsync(year, month, paymentType, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PayslipListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PayslipListItemDto>>> List(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int limit = 24,
        CancellationToken cancellationToken = default)
    {
        var items = await payslipService.ListAsync(year, month, limit, cancellationToken);
        return Ok(items);
    }
}
