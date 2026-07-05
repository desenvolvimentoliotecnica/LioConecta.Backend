using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LioConecta.Api.Auth;

public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, DevAuthDefaults.MariaSilvaName),
            new(ClaimTypes.Email, DevAuthDefaults.MariaSilvaEmail),
            new("preferred_username", DevAuthDefaults.MariaSilvaEmail),
            new("oid", DevAuthDefaults.MariaSilvaObjectId.ToString()),
            new("person_slug", DevAuthDefaults.MariaSilvaSlug),
            new(ClaimTypes.Role, "Employee"),
        };

        var identity = new ClaimsIdentity(claims, DevAuthDefaults.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevAuthDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
