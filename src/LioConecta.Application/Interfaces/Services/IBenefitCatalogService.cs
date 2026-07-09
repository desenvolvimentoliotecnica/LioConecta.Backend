using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IBenefitCatalogService
{
    Task<BenefitManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BenefitCatalogItemDto>> ListAsync(
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<BenefitCatalogItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BenefitCatalogItemDto> CreateAsync(
        UpsertBenefitCatalogRequest request,
        CancellationToken cancellationToken = default);

    Task<BenefitCatalogItemDto> UpdateAsync(
        Guid id,
        UpsertBenefitCatalogRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
