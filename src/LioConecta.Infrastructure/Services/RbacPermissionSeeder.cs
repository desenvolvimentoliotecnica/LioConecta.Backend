using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class RbacPermissionSeeder(AppDbContext db, ILogger<RbacPermissionSeeder> logger)
{
    public async Task EnsurePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var catalog = PermissionCatalog.All();
        var existingKeys = await db.Permissions.AsNoTracking().Select(p => p.Key).ToListAsync(cancellationToken);
        var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var added = 0;

        foreach (var def in catalog)
        {
            if (existingSet.Contains(def.Key))
            {
                continue;
            }

            var parts = def.Key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            db.Permissions.Add(new Permission
            {
                Key = def.Key,
                Module = parts.Length > 0 ? parts[0] : def.Key,
                Resource = parts.Length > 1 ? parts[1] : string.Empty,
                Action = parts.Length > 2 ? string.Join('.', parts.Skip(2)) : (parts.Length > 1 ? parts[^1] : string.Empty),
                Label = def.Label,
                Description = def.Description,
                BusinessArea = def.Area,
                AllowedDataScopesJson = JsonSerializer.Serialize(def.AllowedScopes.Select(s => s.ToString()).ToArray()),
                MenuPath = def.MenuPath,
                IsSystem = true,
                SortOrder = added,
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("RBAC: {Count} permissões adicionadas ao catálogo.", added);
        }
    }

    public async Task EnsureRolesAsync(CancellationToken cancellationToken = default)
    {
        await EnsurePermissionsAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var templates = RoleTemplateCatalog.All();

        foreach (var template in templates)
        {
            var role = await db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Slug == template.Slug, cancellationToken);

            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = template.Name,
                    Slug = template.Slug,
                    Description = template.Description,
                    BusinessArea = template.BusinessArea,
                    IsSystem = template.IsSystem,
                    IsKeyUserTemplate = template.IsKeyUserTemplate,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.Roles.Add(role);
            }
            else
            {
                role.Name = template.Name;
                role.Description = template.Description;
                role.BusinessArea = template.BusinessArea;
                role.IsSystem = template.IsSystem;
                role.IsKeyUserTemplate = template.IsKeyUserTemplate;
                role.UpdatedAt = now;
            }

            var desired = template.Permissions
                .GroupBy(p => p.PermissionKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.MaxBy(x => x.Scope).Scope, StringComparer.OrdinalIgnoreCase);

            foreach (var existing in role.RolePermissions.ToList())
            {
                if (!desired.ContainsKey(existing.PermissionKey))
                {
                    db.RolePermissions.Remove(existing);
                }
            }

            foreach (var (permissionKey, scope) in desired)
            {
                var rp = role.RolePermissions.FirstOrDefault(x => x.PermissionKey == permissionKey);
                if (rp is null)
                {
                    role.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionKey = permissionKey,
                        DataScope = scope,
                    });
                }
                else
                {
                    rp.DataScope = scope;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("RBAC: roles sistema e templates sincronizados.");
    }
}
