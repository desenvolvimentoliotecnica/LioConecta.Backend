using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IUserTeamsTokenRepository
{
    Task<UserTeamsToken?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task UpsertAsync(UserTeamsToken token, CancellationToken cancellationToken = default);

    Task DeleteByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}
