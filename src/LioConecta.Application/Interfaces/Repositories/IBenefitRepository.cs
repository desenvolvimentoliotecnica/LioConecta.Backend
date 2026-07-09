using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IBenefitRepository
{
    Task<IReadOnlyList<EmployeeBenefit>> ListAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<EmployeeBenefit?> GetByKeyAsync(
        Guid personId,
        string benefitKey,
        CancellationToken cancellationToken = default);

    Task<EmployeeBenefit?> GetByKeyIncludingInactiveAsync(
        Guid personId,
        string benefitKey,
        CancellationToken cancellationToken = default);

    Task<EmployeeBenefit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeBenefit>> ListForManagementAsync(
        Guid? personId,
        string? departmentId,
        string? catalogKey,
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task AddAsync(EmployeeBenefit entity, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
