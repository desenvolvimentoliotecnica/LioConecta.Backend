using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task AddAsync(UserPreference preference, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default);
}
