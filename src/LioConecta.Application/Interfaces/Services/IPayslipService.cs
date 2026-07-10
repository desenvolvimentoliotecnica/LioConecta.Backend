using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPayslipService
{
    Task<PayslipSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<PayslipServiceDto> GetServices();

    Task<IReadOnlyList<PayslipListItemDto>> ListAsync(
        int? year,
        int? month,
        int limit,
        CancellationToken cancellationToken = default);

    Task<PayslipDetailDto?> GetDetailAsync(
        int year,
        int month,
        string? paymentType = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetPdfAsync(
        int year,
        int month,
        string? paymentType = null,
        CancellationToken cancellationToken = default);

    Task<PayslipComparativoDto?> GetComparativoAsync(
        int fromYear,
        int fromMonth,
        int toYear,
        int toMonth,
        CancellationToken cancellationToken = default);

    Task<IncomeStatementDto?> GetIncomeStatementAsync(int year, CancellationToken cancellationToken = default);

    Task<FgtsConsultaDto> GetFgtsConsultaAsync(CancellationToken cancellationToken = default);

    Task<DescontosConsultaDto> GetDescontosConsultaAsync(CancellationToken cancellationToken = default);

    Task<RubricasConsultaDto> GetRubricasConsultaAsync(CancellationToken cancellationToken = default);

    Task<byte[]> GetComprovantePdfAsync(CancellationToken cancellationToken = default);

    Task<byte[]> GetCartaConsignacaoPdfAsync(CancellationToken cancellationToken = default);

    Task<PayslipRequestResultDto> CreateRequestAsync(
        CreatePayslipRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PagedPayslipAccessLogDto> GetAccessLogAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        Guid? targetPersonId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
