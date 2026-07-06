using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace LioConecta.Infrastructure.Services;

public sealed class PortalJwtService(IAppSettingsProvider settingsProvider) : IPortalJwtService
{
    private const string Issuer = "LioConecta";
    private const string Audience = "LioConecta.Portal";

    public (string Token, int ExpiresInSeconds) CreateToken(Person person, IReadOnlyList<UserRole> roles)
    {
        var signingKey = settingsProvider.GetString(AppSettingKeys.AuthJwtSigningKey);
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("A chave auth.jwt_signing_key não está configurada.");
        }

        var expiryMinutes = ParseInt(settingsProvider.GetString(AppSettingKeys.AuthJwtExpiryMinutes), 480);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        var claims = new List<Claim>
        {
            new("oid", person.Id.ToString()),
            new("preferred_username", person.Email),
            new("person_slug", person.Slug),
            new(ClaimTypes.Email, person.Email),
            new(ClaimTypes.Name, person.Name),
        };

        foreach (var role in roles.Distinct())
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiryMinutes * 60);
    }

    public static TokenValidationParameters BuildValidationParameters(string signingKey) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };

    private static int ParseInt(string? raw, int fallback) =>
        int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
}
