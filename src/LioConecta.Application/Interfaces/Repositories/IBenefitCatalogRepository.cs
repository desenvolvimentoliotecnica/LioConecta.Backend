using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IBenefitCatalogRepository
{
    Task<IReadOnlyList<BenefitCatalog>> ListAsync(
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<BenefitCatalog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BenefitCatalog?> GetByKeyAsync(string catalogKey, CancellationToken cancellationToken = default);

    Task<bool> KeyExistsAsync(string catalogKey, Guid? excludeId, CancellationToken cancellationToken = default);

    Task AddAsync(BenefitCatalog entity, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
