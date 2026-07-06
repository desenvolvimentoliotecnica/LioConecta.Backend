namespace LioConecta.Application.Interfaces.Services;

public sealed record LdapAuthResult(string Email, string? DisplayName);

public interface ILdapAuthService
{
    Task<LdapAuthResult?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
}
