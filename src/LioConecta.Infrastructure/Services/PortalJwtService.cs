using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace LioConecta.Infrastructure.Services;

public sealed class PortalJwtService(IAppSettingsProvider settingsProvider) : IPortalJwtService
{
    private const string Issuer = "LioConecta";
    private const string Audience = "LioConecta.Portal";
    private const int MaxPermissionClaimLength = 3500;

    public (string Token, int ExpiresInSeconds) CreateToken(Person person, IReadOnlyList<UserRole> roles)
        => CreateToken(person, roles, RbacSubjectType.Person, person.Id, person.Id.ToString("N"), false, []);

    public (string Token, int ExpiresInSeconds) CreateToken(
        Person person,
        IReadOnlyList<UserRole> roles,
        RbacSubjectType subjectType,
        Guid subjectId,
        string securityStamp,
        bool isTestUser,
        IReadOnlyList<EffectivePermissionDto> permissions)
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
            new("sub_type", MapSubjectTypeClaim(subjectType, isTestUser)),
            new("sub_id", subjectId.ToString()),
            new("sst", securityStamp),
            new("is_test_user", isTestUser ? "true" : "false"),
        };

        foreach (var role in roles.Distinct())
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var permJson = BuildPermissionsClaim(permissions);
        if (!string.IsNullOrWhiteSpace(permJson))
        {
            claims.Add(new Claim("perm", permJson));
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

    private static string MapSubjectTypeClaim(RbacSubjectType subjectType, bool isTestUser)
    {
        if (isTestUser || subjectType == RbacSubjectType.TestUser)
        {
            return "test";
        }

        return subjectType switch
        {
            RbacSubjectType.PortalUser => "portal",
            RbacSubjectType.Person => "ldap",
            _ => subjectType.ToString().ToLowerInvariant(),
        };
    }

    private static string? BuildPermissionsClaim(IReadOnlyList<EffectivePermissionDto> permissions)
    {
        if (permissions.Count == 0)
        {
            return null;
        }

        var compact = permissions
            .Select(p => new { k = p.Key, s = p.Scope.ToString() })
            .ToList();

        var json = JsonSerializer.Serialize(compact);
        return json.Length <= MaxPermissionClaimLength ? json : null;
    }

    private static int ParseInt(string? raw, int fallback) =>
        int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
}
