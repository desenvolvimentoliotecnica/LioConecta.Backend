namespace LioConecta.Application.DTOs;

public sealed record PayslipSummaryDto(
    string LatestCompetence,
    decimal LatestNetAmount,
    int HistoryCount);

public sealed record PayslipServiceDto(
    string Id,
    string Title,
    string Desc,
    string Category,
    string Sla,
    bool Online,
    bool Featured,
    string Action,
    string HelpText);

public sealed record PayslipLineDto(
    string Code,
    string Label,
    decimal Amount,
    decimal? Quantity);

public sealed record PayslipListItemDto(
    int Year,
    int Month,
    string Competence,
    decimal GrossAmount,
    decimal NetAmount,
    DateTimeOffset PublishedAt);

public sealed record PayslipDetailDto(
    int Year,
    int Month,
    string Competence,
    decimal GrossAmount,
    decimal NetAmount,
    decimal DeductionsTotal,
    IReadOnlyList<PayslipLineDto> Earnings,
    IReadOnlyList<PayslipLineDto> Deductions,
    DateTimeOffset PublishedAt);

public sealed record PayslipComparativoDto(
    PayslipDetailDto From,
    PayslipDetailDto To,
    decimal NetDifference,
    decimal GrossDifference);

public sealed record FgtsDepositDto(
    string Competence,
    decimal Amount,
    decimal EmployerShare);

public sealed record FgtsConsultaDto(
    decimal TotalBalance,
    IReadOnlyList<FgtsDepositDto> Deposits);

public sealed record DescontoItemDto(
    string Code,
    string Label,
    decimal Amount,
    string Competence);

public sealed record DescontosConsultaDto(
    decimal TotalMonthly,
    IReadOnlyList<DescontoItemDto> Items);

public sealed record RubricaHelpDto(
    string Code,
    string Label,
    string Description);

public sealed record RubricasConsultaDto(
    IReadOnlyList<RubricaHelpDto> Items);

public sealed record IncomeStatementLineDto(
    int Month,
    decimal Paid,
    decimal Withheld);

public sealed record IncomeStatementDto(
    int Year,
    decimal TotalPaid,
    decimal TotalWithheld,
    IReadOnlyList<IncomeStatementLineDto> Lines);

public sealed record CreatePayslipRequestDto(
    string ServiceId,
    string? Competence,
    string? Notes);

public sealed record PayslipRequestResultDto(
    Guid RequestId,
    string Status,
    string Message);
