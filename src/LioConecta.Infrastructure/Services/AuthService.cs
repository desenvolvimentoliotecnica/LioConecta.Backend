using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Common.Observability;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Integrations.Ldap;
using LioConecta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class AuthService(
    AppDbContext db,
    ILdapAuthService ldapAuthService,
    IPortalJwtService portalJwtService,
    IAppSettingsProvider settingsProvider,
    IAccessAuditRecorder accessAuditRecorder,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new UnauthorizedAccessException("E-mail e senha são obrigatórios.");
        }

        Person person;
        IReadOnlyList<UserRole> roles;

        var portalUser = await db.PortalUsers
            .Include(u => u.Person)
            .FirstOrDefaultAsync(
                u => u.IsActive && u.Email.ToLower() == email,
                cancellationToken);

        if (portalUser is not null)
        {
            if (BCrypt.Net.BCrypt.Verify(password, portalUser.PasswordHash))
            {
                person = portalUser.Person;
                roles = ParseRoles(portalUser.RolesJson);
                logger.LogInformation("Login local bem-sucedido para {Email}.", email);
            }
            else
            {
                var ldapResult = await ldapAuthService.AuthenticateAsync(email, password, cancellationToken);
                if (ldapResult is null)
                {
                    await RecordLoginFailedAsync(email, cancellationToken);
                    throw new UnauthorizedAccessException("Credenciais inválidas.");
                }

                person = portalUser.Person ?? await ResolveOrCreatePersonAsync(ldapResult, cancellationToken);
                roles = portalUser.IsSuperAdmin
                    ? ParseRoles(portalUser.RolesJson)
                    : MergeRoles(ResolveLdapRoles(ldapResult.Email), ParseRoles(portalUser.RolesJson));
                logger.LogInformation(
                    "Login LDAP bem-sucedido para {Email} (conta local também cadastrada).",
                    email);
            }
        }
        else
        {
            var ldapResult = await ldapAuthService.AuthenticateAsync(email, password, cancellationToken);
            if (ldapResult is null)
            {
                await RecordLoginFailedAsync(email, cancellationToken);
                throw new UnauthorizedAccessException("Credenciais inválidas.");
            }

            person = await ResolveOrCreatePersonAsync(ldapResult, cancellationToken);
            roles = ResolveLdapRoles(ldapResult.Email);
            logger.LogInformation("Login LDAP bem-sucedido para {Email}.", email);
        }

        var (token, expiresInSeconds) = portalJwtService.CreateToken(person, roles);
        await RecordLoginSucceededAsync(person.Email, cancellationToken);

        return new LoginResponse(token, expiresInSeconds, PersonMapper.ToMe(person, roles));
    }

    public Task RecordLogoutAsync(CancellationToken cancellationToken = default) =>
        RecordLogoutEventAsync(cancellationToken);

    private async Task<Person> ResolveOrCreatePersonAsync(
        LdapAuthResult ldapResult,
        CancellationToken cancellationToken)
    {
        var email = ldapResult.Email.Trim().ToLowerInvariant();
        var existing = await db.People.FirstOrDefaultAsync(p => p.Email.ToLower() == email, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var displayName = string.IsNullOrWhiteSpace(ldapResult.DisplayName)
            ? email.Split('@')[0].Replace('.', ' ')
            : ldapResult.DisplayName.Trim();

        var person = new Person
        {
            Id = Guid.NewGuid(),
            Slug = await GenerateUniqueSlugAsync(displayName, cancellationToken),
            Name = displayName,
            Email = email,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.People.Add(person);
        await db.SaveChangesAsync(cancellationToken);
        return person;
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        var suffix = 1;

        while (await db.People.AnyAsync(p => p.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? $"user-{Guid.NewGuid():N}"[..12] : slug;
    }

    private IReadOnlyList<UserRole> ResolveLdapRoles(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var superAdmins = ParseEmailList(settingsProvider.GetString(AppSettingKeys.AuthSuperAdminEmails));
        if (superAdmins.Contains(normalized))
        {
            return [UserRole.Admin, UserRole.Employee];
        }

        return [UserRole.Employee];
    }

    private static IReadOnlyList<UserRole> MergeRoles(
        IReadOnlyList<UserRole> primary,
        IReadOnlyList<UserRole> secondary) =>
        primary.Concat(secondary).Distinct().DefaultIfEmpty(UserRole.Employee).ToList();

    private static IReadOnlyList<UserRole> ParseRoles(string rolesJson)
    {
        try
        {
            var roles = JsonSerializer.Deserialize<List<string>>(rolesJson) ?? [];
            return roles
                .Select(role => Enum.TryParse<UserRole>(role, true, out var parsed) ? parsed : (UserRole?)null)
                .Where(role => role.HasValue)
                .Select(role => role!.Value)
                .Distinct()
                .DefaultIfEmpty(UserRole.Employee)
                .ToList();
        }
        catch
        {
            return [UserRole.Employee];
        }
    }

    private static HashSet<string> ParseEmailList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var emails = JsonSerializer.Deserialize<List<string>>(raw) ?? [];
            return emails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task RecordLoginFailedAsync(string email, CancellationToken cancellationToken)
    {
        await accessAuditRecorder.RecordAsync(new AccessAuditEntry(
            EventType: AccessEventTypes.Authentication,
            EventName: ObservabilityEventNames.Authentication.LoginFailed,
            CorrelationId: Guid.NewGuid(),
            UserId: null,
            UsernameSnapshot: email,
            SessionId: null,
            Resource: "/api/v1/auth/login",
            Action: "login",
            Result: AccessEventResults.Failed,
            ReasonCode: "invalid_credentials",
            StatusCode: StatusCodes.Status401Unauthorized,
            HttpMethod: "POST",
            Path: "/api/v1/auth/login"),
            cancellationToken);
    }

    private async Task RecordLoginSucceededAsync(string email, CancellationToken cancellationToken)
    {
        await accessAuditRecorder.RecordAsync(new AccessAuditEntry(
            EventType: AccessEventTypes.Authentication,
            EventName: ObservabilityEventNames.Authentication.LoginSucceeded,
            CorrelationId: Guid.NewGuid(),
            UserId: null,
            UsernameSnapshot: email,
            SessionId: null,
            Resource: "/api/v1/auth/login",
            Action: "login",
            Result: AccessEventResults.Success,
            ReasonCode: null,
            StatusCode: StatusCodes.Status200OK,
            HttpMethod: "POST",
            Path: "/api/v1/auth/login"),
            cancellationToken);
    }

    private async Task RecordLogoutEventAsync(CancellationToken cancellationToken)
    {
        await accessAuditRecorder.RecordAsync(new AccessAuditEntry(
            EventType: AccessEventTypes.Authentication,
            EventName: ObservabilityEventNames.Authentication.Logout,
            CorrelationId: Guid.NewGuid(),
            UserId: null,
            UsernameSnapshot: null,
            SessionId: null,
            Resource: "/api/v1/auth/logout",
            Action: "logout",
            Result: AccessEventResults.Success,
            ReasonCode: null,
            StatusCode: StatusCodes.Status204NoContent,
            HttpMethod: "POST",
            Path: "/api/v1/auth/logout"),
            cancellationToken);
    }
}
