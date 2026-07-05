using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IComunicadoHeroImageRepository
{
    Task<int> GetMaxVersionAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task AddAsync(ComunicadoHeroImage image, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComunicadoHeroImage>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
