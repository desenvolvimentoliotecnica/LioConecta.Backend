using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IBenefitRepository
{
    Task<IReadOnlyList<EmployeeBenefit>> ListAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<EmployeeBenefit?> GetByKeyAsync(
        Guid personId,
        string benefitKey,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(Guid personId, CancellationToken cancellationToken = default);
}
