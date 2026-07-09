using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IBenefitManagementService
{
    Task<BenefitsBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);

    Task<BenefitManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BenefitManagementListItemDto>> ListManagementAsync(
        Guid? personId,
        string? departmentId,
        string? catalogKey,
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<BenefitEmployeeDetailDto?> GetManagementDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BenefitEmployeeDetailDto> CreateAsync(
        UpsertEmployeeBenefitRequest request,
        CancellationToken cancellationToken = default);

    Task<BenefitEmployeeDetailDto> UpdateAsync(
        Guid id,
        UpsertEmployeeBenefitRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BenefitEmployeeDetailDto> AssignFromCatalogAsync(
        AssignBenefitFromCatalogRequest request,
        CancellationToken cancellationToken = default);

    Task<BulkBenefitOperationResultDto> BulkAssignFromCatalogAsync(
        BulkAssignBenefitsRequest request,
        CancellationToken cancellationToken = default);

    Task<BulkBenefitOperationResultDto> BulkSetActiveAsync(
        BulkSetActiveBenefitsRequest request,
        CancellationToken cancellationToken = default);

    Task<BulkBenefitPreviewDto> BulkPreviewAsync(
        string operation,
        BulkBenefitTargetRequest target,
        string? catalogKey,
        string? onDuplicate,
        CancellationToken cancellationToken = default);
}
