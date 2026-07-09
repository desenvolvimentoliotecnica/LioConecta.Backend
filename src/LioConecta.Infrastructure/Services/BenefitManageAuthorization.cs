using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

internal static class BenefitManageAuthorization
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal static readonly IReadOnlyList<string> Categories =
        ["saude", "alimentacao", "mobilidade", "qualidade", "familia"];

    internal static readonly IReadOnlyList<string> Statuses =
        ["obrigatorio", "opcional", "flexivel"];

    public static async Task<bool> CanManageAsync(
        AppDbContext db,
        ICurrentUserService currentUserService,
        IAppSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        if (roles.Contains(UserRole.Admin))
        {
            return true;
        }

        var allowedRoles = DeserializeRoles(settingsProvider.GetString(AppSettingKeys.BenefitsAllowedRoles));
        if (roles.Any(role => allowedRoles.Contains(role)))
        {
            return true;
        }

        var allowedEmails = settingsProvider
            .GetStringArray(AppSettingKeys.BenefitsAllowedEmails)
            .Select(email => email.Trim().ToLowerInvariant())
            .Where(email => email.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowedEmails.Count == 0)
        {
            return false;
        }

        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var email = await db.People
            .AsNoTracking()
            .Where(person => person.Id == personId)
            .Select(person => person.Email)
            .FirstOrDefaultAsync(cancellationToken);

        return email is not null && allowedEmails.Contains(email.Trim().ToLowerInvariant());
    }

    public static async Task EnsureCanManageAsync(
        AppDbContext db,
        ICurrentUserService currentUserService,
        IAppSettingsProvider settingsProvider,
        CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(db, currentUserService, settingsProvider, cancellationToken))
        {
            throw new UnauthorizedAccessException("Sem permissão para gerir benefícios.");
        }
    }

    private static IReadOnlyList<UserRole> DeserializeRoles(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [UserRole.HR];
        }

        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            var roles = new List<UserRole>();
            foreach (var value in values)
            {
                if (Enum.TryParse<UserRole>(value, true, out var role))
                {
                    roles.Add(role);
                }
            }

            return roles.Count > 0 ? roles : [UserRole.HR];
        }
        catch (JsonException)
        {
            return [UserRole.HR];
        }
    }
}
