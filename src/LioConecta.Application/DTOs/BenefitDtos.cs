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

public sealed record BenefitManagePolicyDto(bool CanManage);

public sealed record BenefitDepartmentOptionDto(
    string Id,
    string Name,
    int Count);

public sealed record BenefitsBootstrapDto(
    bool CanManage,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<BenefitDepartmentOptionDto> Departments,
    int CatalogCount);

public sealed record BenefitCatalogItemDto(
    Guid Id,
    string CatalogKey,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    bool IsActive,
    string? PortalUrl,
    string HelpText,
    decimal? DefaultMonthlyValue,
    int SortOrder,
    IReadOnlyList<BenefitDetailLineDto> Lines,
    IReadOnlyList<BenefitDependentDto> Dependents,
    IReadOnlyList<string> Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpsertBenefitCatalogRequest(
    string CatalogKey,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    bool IsActive,
    string? PortalUrl,
    string HelpText,
    decimal? DefaultMonthlyValue,
    int SortOrder,
    IReadOnlyList<BenefitDetailLineDto>? Lines = null,
    IReadOnlyList<BenefitDependentDto>? Dependents = null,
    IReadOnlyList<string>? Notes = null);

public sealed record BenefitManagementListItemDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string? DepartmentName,
    string BenefitKey,
    string Title,
    string Category,
    string Provider,
    string Status,
    bool IsActive,
    decimal? MonthlyValue,
    DateTimeOffset UpdatedAt);

public sealed record BenefitEmployeeDetailDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string? DepartmentName,
    string BenefitKey,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    bool IsActive,
    string? PortalUrl,
    string HelpText,
    decimal? MonthlyValue,
    IReadOnlyList<BenefitDetailLineDto> Lines,
    IReadOnlyList<BenefitDependentDto> Dependents,
    IReadOnlyList<string> Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpsertEmployeeBenefitRequest(
    Guid PersonId,
    string BenefitKey,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    bool IsActive,
    string? PortalUrl,
    string HelpText,
    decimal? MonthlyValue,
    IReadOnlyList<BenefitDetailLineDto>? Lines,
    IReadOnlyList<BenefitDependentDto>? Dependents,
    IReadOnlyList<string>? Notes);

public sealed record AssignBenefitFromCatalogRequest(
    Guid PersonId,
    string CatalogKey,
    BenefitAssignmentOverridesDto? Overrides);

public sealed record BenefitAssignmentOverridesDto(
    decimal? MonthlyValue,
    bool? IsActive,
    IReadOnlyList<BenefitDetailLineDto>? Lines,
    IReadOnlyList<BenefitDependentDto>? Dependents,
    IReadOnlyList<string>? Notes);

public sealed record BulkBenefitTargetRequest(
    IReadOnlyList<Guid>? PersonIds,
    IReadOnlyList<string>? DepartmentIds,
    IReadOnlyList<Guid>? ExcludePersonIds);

public sealed record BulkAssignBenefitsRequest(
    BulkBenefitTargetRequest Target,
    string CatalogKey,
    BenefitAssignmentOverridesDto? Overrides,
    string? OnDuplicate);

public sealed record BulkSetActiveBenefitsRequest(
    BulkBenefitTargetRequest Target,
    string? CatalogKey,
    bool IsActive);

public sealed record BulkBenefitOperationErrorDto(
    Guid PersonId,
    string Message);

public sealed record BulkBenefitOperationResultDto(
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<BulkBenefitOperationErrorDto> Errors);

public sealed record BulkBenefitPreviewPersonDto(
    Guid Id,
    string Name);

public sealed record BulkBenefitPreviewDto(
    int TargetPeopleCount,
    int MatchingBenefitsCount,
    int WouldCreate,
    int WouldUpdate,
    int WouldSkip,
    IReadOnlyList<BulkBenefitPreviewPersonDto> SamplePeople);
