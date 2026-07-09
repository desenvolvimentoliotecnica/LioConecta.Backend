using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class RbacMigrationSeeder(
    AppDbContext db,
    RbacPermissionSeeder permissionSeeder,
    ILogger<RbacMigrationSeeder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await permissionSeeder.EnsureRolesAsync(cancellationToken);
        await MigratePortalUserRolesAsync(cancellationToken);
        await MigrateAppSettingsLegacyAsync(cancellationToken);
        await MigrateSuperAdminEmailsAsync(cancellationToken);
    }

    private async Task MigratePortalUserRolesAsync(CancellationToken cancellationToken)
    {
        var portalUsers = await db.PortalUsers.AsNoTracking().ToListAsync(cancellationToken);
        if (portalUsers.Count == 0)
        {
            return;
        }

        var roleBySlug = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Slug, r => r.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var added = 0;

        foreach (var user in portalUsers)
        {
            var roles = ParseRoles(user.RolesJson);
            foreach (var roleName in roles)
            {
                var slug = roleName.ToString();
                if (!roleBySlug.TryGetValue(slug, out var roleId))
                {
                    continue;
                }

                var exists = await db.SubjectRoleAssignments.AnyAsync(
                    a => a.SubjectType == RbacSubjectType.PortalUser
                         && a.SubjectId == user.Id
                         && a.RoleId == roleId,
                    cancellationToken);

                if (exists)
                {
                    continue;
                }

                db.SubjectRoleAssignments.Add(new SubjectRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    SubjectType = RbacSubjectType.PortalUser,
                    SubjectId = user.Id,
                    RoleId = roleId,
                    AssignedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                added++;
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("RBAC: {Count} atribuições migradas de PortalUser.RolesJson.", added);
        }
    }

    private async Task MigrateAppSettingsLegacyAsync(CancellationToken cancellationToken)
    {
        var mappings = new (string SettingKey, string RoleSlug)[]
        {
            (AppSettingKeys.BenefitsAllowedRoles, "HR"),
            (AppSettingKeys.SystemsAllowedRoles, "TI"),
            (AppSettingKeys.RamaisAllowedRoles, "TI"),
            (AppSettingKeys.LoopAllowedRoles, "KeyUser-Projetos"),
            (AppSettingKeys.CompassAllowedRoles, "KeyUser-Contabil"),
            (AppSettingKeys.FacilitiesMenuAllowedRoles, "KeyUser-Facilities"),
            (AppSettingKeys.UniLioAllowedRoles, "Employee"),
        };

        var roleBySlug = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Slug, r => r.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var settings = await db.AppSettings.AsNoTracking().ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var (settingKey, defaultRoleSlug) in mappings)
        {
            var setting = settings.FirstOrDefault(s => s.Key == settingKey);
            if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
            {
                continue;
            }

            var roleNames = ParseStringList(setting.Value);
            foreach (var roleName in roleNames)
            {
                if (!roleBySlug.TryGetValue(roleName, out var roleId))
                {
                    continue;
                }

                await EnsureLegacyRoleHasAssignmentsFromEmailsAsync(settingKey.Replace(".allowed_roles", ".allowed_emails"), roleId, settings, now, cancellationToken);
            }

            if (roleNames.Count == 0 && roleBySlug.TryGetValue(defaultRoleSlug, out var fallbackRoleId))
            {
                await EnsureLegacyRoleHasAssignmentsFromEmailsAsync(settingKey.Replace(".allowed_roles", ".allowed_emails"), fallbackRoleId, settings, now, cancellationToken);
            }
        }
    }

    private async Task EnsureLegacyRoleHasAssignmentsFromEmailsAsync(
        string emailsKey,
        Guid roleId,
        List<AppSetting> settings,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var emailsSetting = settings.FirstOrDefault(s => s.Key == emailsKey);
        if (emailsSetting is null)
        {
            return;
        }

        var emails = ParseStringList(emailsSetting.Value);
        foreach (var email in emails)
        {
            var person = await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower(), cancellationToken);
            if (person is null)
            {
                continue;
            }

            var exists = await db.SubjectRoleAssignments.AnyAsync(
                a => a.SubjectType == RbacSubjectType.Person && a.SubjectId == person.Id && a.RoleId == roleId,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            db.SubjectRoleAssignments.Add(new SubjectRoleAssignment
            {
                Id = Guid.NewGuid(),
                SubjectType = RbacSubjectType.Person,
                SubjectId = person.Id,
                RoleId = roleId,
                AssignedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MigrateSuperAdminEmailsAsync(CancellationToken cancellationToken)
    {
        var setting = await db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == AppSettingKeys.AuthSuperAdminEmails, cancellationToken);
        if (setting is null)
        {
            return;
        }

        var adminRoleId = await db.Roles.AsNoTracking()
            .Where(r => r.Slug == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId == Guid.Empty)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var email in ParseStringList(setting.Value))
        {
            var person = await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower(), cancellationToken);
            if (person is null)
            {
                continue;
            }

            var exists = await db.SubjectRoleAssignments.AnyAsync(
                a => a.SubjectType == RbacSubjectType.Person && a.SubjectId == person.Id && a.RoleId == adminRoleId,
                cancellationToken);

            if (!exists)
            {
                db.SubjectRoleAssignments.Add(new SubjectRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    SubjectType = RbacSubjectType.Person,
                    SubjectId = person.Id,
                    RoleId = adminRoleId,
                    AssignedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<UserRole> ParseRoles(string rolesJson)
    {
        try
        {
            var roles = JsonSerializer.Deserialize<List<string>>(rolesJson, JsonOptions) ?? [];
            return roles
                .Select(role => Enum.TryParse<UserRole>(role, true, out var parsed) ? parsed : (UserRole?)null)
                .Where(role => role.HasValue)
                .Select(role => role!.Value)
                .Distinct()
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ParseStringList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw, JsonOptions)?
                       .Where(v => !string.IsNullOrWhiteSpace(v))
                       .Select(v => v.Trim())
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
    }
}
