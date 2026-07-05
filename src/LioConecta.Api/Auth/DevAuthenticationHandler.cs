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
            new(ClaimTypes.Name, DevAuthDefaults.DevUserName),
            new(ClaimTypes.Email, DevAuthDefaults.DevUserEmail),
            new("preferred_username", DevAuthDefaults.DevUserEmail),
            new("oid", DevAuthDefaults.DevUserObjectId.ToString()),
            new("person_slug", DevAuthDefaults.DevUserSlug),
            new(ClaimTypes.Role, "Employee"),
            new(ClaimTypes.Role, "Admin"),
        };

        var identity = new ClaimsIdentity(claims, DevAuthDefaults.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevAuthDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
