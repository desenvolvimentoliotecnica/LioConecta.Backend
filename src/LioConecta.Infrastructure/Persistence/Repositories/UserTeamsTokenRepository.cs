using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class UserTeamsTokenRepository(AppDbContext db) : IUserTeamsTokenRepository
{
    public Task<UserTeamsToken?> GetByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        db.UserTeamsTokens
            .FirstOrDefaultAsync(t => t.PersonId == personId, cancellationToken);

    public async Task UpsertAsync(UserTeamsToken token, CancellationToken cancellationToken = default)
    {
        var existing = await db.UserTeamsTokens
            .FirstOrDefaultAsync(t => t.PersonId == token.PersonId, cancellationToken);

        if (existing is null)
        {
            db.UserTeamsTokens.Add(token);
        }
        else
        {
            existing.EncryptedAccessToken = token.EncryptedAccessToken;
            existing.EncryptedRefreshToken = token.EncryptedRefreshToken;
            existing.ExpiresAt = token.ExpiresAt;
            existing.ScopesJson = token.ScopesJson;
            existing.UpdatedAt = token.UpdatedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var existing = await db.UserTeamsTokens
            .FirstOrDefaultAsync(t => t.PersonId == personId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        db.UserTeamsTokens.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }
}
