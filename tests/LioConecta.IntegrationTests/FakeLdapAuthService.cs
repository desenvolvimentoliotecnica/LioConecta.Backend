using LioConecta.Application.Interfaces.Services;

namespace LioConecta.IntegrationTests;

public sealed class FakeLdapAuthService : ILdapAuthService
{
    public const string LdapUserEmail = "ldap.user@liotecnica.com.br";
    public const string LdapUserPassword = "ldap-pass";

    public Task<LdapAuthResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (password != LdapUserPassword)
        {
            return Task.FromResult<LdapAuthResult?>(null);
        }

        if (string.Equals(email, LdapUserEmail, StringComparison.OrdinalIgnoreCase)
            || string.Equals(email, "leonardo.mendes@liotecnica.com.br", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<LdapAuthResult?>(
                new LdapAuthResult(email, "LDAP Test User"));
        }

        return Task.FromResult<LdapAuthResult?>(null);
    }
}
