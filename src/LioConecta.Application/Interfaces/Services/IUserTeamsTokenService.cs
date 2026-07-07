namespace LioConecta.Application.Interfaces.Services;

public interface IUserTeamsTokenService
{
    Task<bool> HasLinkedAccountAsync(Guid personId, CancellationToken cancellationToken = default);

    Task StoreTokensAsync(
        Guid personId,
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAt,
        IReadOnlyList<string>? scopes,
        CancellationToken cancellationToken = default);

    Task<string> GetValidAccessTokenAsync(Guid personId, CancellationToken cancellationToken = default);

    Task UnlinkAsync(Guid personId, CancellationToken cancellationToken = default);
}
