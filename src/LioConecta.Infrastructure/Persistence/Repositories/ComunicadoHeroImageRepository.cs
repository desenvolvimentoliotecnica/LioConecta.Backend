using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class ComunicadoHeroImageRepository(AppDbContext db) : IComunicadoHeroImageRepository
{
    public async Task<int> GetMaxVersionAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var max = await db.ComunicadoHeroImages
            .Where(i => i.AssetId == assetId)
            .Select(i => (int?)i.Version)
            .MaxAsync(cancellationToken);

        return max ?? 0;
    }

    public async Task AddAsync(ComunicadoHeroImage image, CancellationToken cancellationToken = default)
    {
        db.ComunicadoHeroImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ComunicadoHeroImage>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await db.ComunicadoHeroImages
            .Include(i => i.UploadedBy)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Version)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
