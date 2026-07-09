using System.Security.Claims;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class PermissionService(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db) : IPermissionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private IReadOnlyList<EffectivePermissionDto>? _cachedPermissions;
    private RbacAuthContext? _cachedContext;

    public async Task<RbacAuthContext?> GetAuthContextAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedContext is not null)
        {
            return _cachedContext;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var user = httpContext.User;
        var subjectTypeRaw = user.FindFirstValue("sub_type");
        var subjectIdRaw = user.FindFirstValue("sub_id");
        var securityStamp = user.FindFirstValue("sst") ?? string.Empty;
        var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = user.FindFirstValue(ClaimTypes.Name) ?? email;
        var isTestUser = string.Equals(subjectTypeRaw, "test", StringComparison.OrdinalIgnoreCase);

        if (Enum.TryParse<RbacSubjectType>(subjectTypeRaw, true, out var subjectType)
            && Guid.TryParse(subjectIdRaw, out var subjectId))
        {
            Guid? personId = null;
            if (isTestUser)
            {
                var testUser = await db.TestUsers.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == subjectId, cancellationToken);
                personId = testUser?.OptionalPersonId;
            }
            else if (Guid.TryParse(user.FindFirstValue("oid"), out var oid))
            {
                personId = oid;
            }

            _cachedContext = new RbacAuthContext(subjectType, subjectId, personId, email, name, securityStamp, isTestUser);
            return _cachedContext;
        }

        var personIdFromOid = user.FindFirstValue("oid");
        if (Guid.TryParse(personIdFromOid, out var ldapPersonId))
        {
            _cachedContext = new RbacAuthContext(
                RbacSubjectType.Person,
                ldapPersonId,
                ldapPersonId,
                email,
                name,
                securityStamp,
                false);
            return _cachedContext;
        }

        return null;
    }

    public async Task<IReadOnlyList<EffectivePermissionDto>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedPermissions is not null)
        {
            return _cachedPermissions;
        }

        var httpContext = httpContextAccessor.HttpContext;
        var permClaim = httpContext?.User.FindFirstValue("perm");
        if (!string.IsNullOrWhiteSpace(permClaim))
        {
            try
            {
                var compact = JsonSerializer.Deserialize<List<CompactPermission>>(permClaim, JsonOptions);
                if (compact is { Count: > 0 })
                {
                    _cachedPermissions = MergePermissions(compact
                        .Where(p => !string.IsNullOrWhiteSpace(p.K))
                        .Select(p => new EffectivePermissionDto(
                            p.K!,
                            Enum.TryParse<DataScope>(p.S, true, out var scope) ? scope : DataScope.Self))
                        .ToList());
                    return _cachedPermissions;
                }
            }
            catch (JsonException)
            {
                // fallback to DB
            }
        }

        var context = await GetAuthContextAsync(cancellationToken);
        if (context is null)
        {
            return [];
        }

        var roleIds = await db.SubjectRoleAssignments.AsNoTracking()
            .Where(a => a.SubjectType == context.SubjectType && a.SubjectId == context.SubjectId)
            .Select(a => a.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0 && context.SubjectType == RbacSubjectType.Person)
        {
            var employeeRoleId = await db.Roles.AsNoTracking()
                .Where(r => r.Slug == "Employee")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (employeeRoleId != Guid.Empty)
            {
                roleIds.Add(employeeRoleId);
            }
        }

        var rolePermissions = await db.RolePermissions.AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => new { rp.PermissionKey, rp.DataScope })
            .ToListAsync(cancellationToken);

        _cachedPermissions = MergePermissions(rolePermissions
            .Select(rp => new EffectivePermissionDto(rp.PermissionKey, rp.DataScope))
            .ToList());

        return _cachedPermissions;
    }

    public async Task<bool> HasPermissionAsync(string permissionKey, DataScope? requiredScope = null, CancellationToken cancellationToken = default)
    {
        var permissions = await GetEffectivePermissionsAsync(cancellationToken);
        var match = permissions.FirstOrDefault(p => p.Key == permissionKey);
        if (match is null)
        {
            return false;
        }

        if (requiredScope is null)
        {
            return true;
        }

        return ScopeRank(match.Scope) >= ScopeRank(requiredScope.Value);
    }

    public async Task EnsurePermissionAsync(string permissionKey, DataScope? requiredScope = null, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(permissionKey, requiredScope, cancellationToken))
        {
            throw new UnauthorizedAccessException($"Permissão necessária: {permissionKey}.");
        }
    }

    public async Task<RbacBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await GetEffectivePermissionsAsync(cancellationToken);
        var context = await GetAuthContextAsync(cancellationToken);
        var menus = PermissionCatalog.All()
            .Where(p => !string.IsNullOrWhiteSpace(p.MenuPath))
            .GroupBy(p => p.MenuPath!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.OrdinalIgnoreCase);

        BusinessArea? area = null;
        if (context?.IsTestUser == true)
        {
            area = await db.TestUsers.AsNoTracking()
                .Where(t => t.Id == context.SubjectId)
                .Select(t => (BusinessArea?)t.BusinessArea)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new RbacBootstrapDto(
            permissions,
            menus,
            context?.SubjectType.ToString().ToLowerInvariant(),
            context?.IsTestUser ?? false,
            area);
    }

    internal static IReadOnlyList<EffectivePermissionDto> MergePermissions(IEnumerable<EffectivePermissionDto> source)
    {
        var map = new Dictionary<string, DataScope>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            if (!map.TryGetValue(item.Key, out var existing) || ScopeRank(item.Scope) > ScopeRank(existing))
            {
                map[item.Key] = item.Scope;
            }
        }

        return map.Select(kv => new EffectivePermissionDto(kv.Key, kv.Value)).OrderBy(p => p.Key).ToList();
    }

    private static int ScopeRank(DataScope scope) => scope switch
    {
        DataScope.Self => 0,
        DataScope.Team => 1,
        DataScope.Department => 2,
        DataScope.Global => 3,
        _ => 0,
    };

    private sealed class CompactPermission
    {
        public string? K { get; set; }
        public string? S { get; set; }
    }
}
