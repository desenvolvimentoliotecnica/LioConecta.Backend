using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class UserPreferenceRepository(AppDbContext db) : IUserPreferenceRepository
{
    public Task<UserPreference?> GetByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);

    public async Task AddAsync(UserPreference preference, CancellationToken cancellationToken = default)
    {
        db.UserPreferences.Add(preference);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default)
    {
        db.UserPreferences.Update(preference);
        await db.SaveChangesAsync(cancellationToken);
    }
}
