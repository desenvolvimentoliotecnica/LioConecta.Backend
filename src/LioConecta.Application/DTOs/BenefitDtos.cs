namespace LioConecta.Application.DTOs;

public sealed record BenefitSummaryDto(
    int ActiveCount,
    decimal TotalMonthlyValue,
    int DependentsCount);

public sealed record BenefitListItemDto(
    string Id,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    bool IsActive,
    string? PortalUrl,
    decimal? MonthlyValue);

public sealed record BenefitDetailLineDto(
    string Label,
    decimal? Amount,
    string? Note);

public sealed record BenefitDependentDto(
    string Name,
    string Relation,
    decimal? MonthlyValue);

public sealed record BenefitDetailDto(
    string Id,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    string? PortalUrl,
    string HelpText,
    decimal? MonthlyValue,
    IReadOnlyList<BenefitDetailLineDto> Lines,
    IReadOnlyList<BenefitDependentDto> Dependents,
    IReadOnlyList<string> Notes);

public sealed record CreateBenefitRequestDto(
    string BenefitId,
    string? Notes);

public sealed record BenefitRequestResultDto(
    Guid RequestId,
    string Status,
    string Message);
