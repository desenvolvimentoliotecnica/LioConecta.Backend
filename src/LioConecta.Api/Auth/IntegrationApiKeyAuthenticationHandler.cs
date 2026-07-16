using System.Security.Claims;
using System.Text.Encodings.Web;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LioConecta.Api.Auth;

public static class IntegrationApiKeyDefaults
{
    public const string SchemeName = "IntegrationApiKey";
    public const string HeaderName = "X-API-Key";
    public const string ServiceClaimType = "integration_service";
}

/// <summary>
/// Authenticates sibling apps (UniLio, Compass, …) via X-API-Key matching app_settings integration.api_key.
/// </summary>
public sealed class IntegrationApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAppSettingsProvider settingsProvider)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(IntegrationApiKeyDefaults.HeaderName, out var provided)
            || string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var expected = settingsProvider.GetString(AppSettingKeys.IntegrationApiKey, string.Empty);
        if (string.IsNullOrWhiteSpace(expected)
            || !FixedTimeEquals(expected.Trim(), provided.ToString().Trim()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid integration API key."));
        }

        var identity = new ClaimsIdentity(Scheme.Name);
        identity.AddClaim(new Claim(ClaimTypes.Name, "integration-service"));
        identity.AddClaim(new Claim(IntegrationApiKeyDefaults.ServiceClaimType, "true"));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
