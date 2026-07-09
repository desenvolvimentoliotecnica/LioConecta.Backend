using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class RbacAdminService(
    AppDbContext db,
    ICurrentUserService currentUserService,
    IPermissionService permissionService,
    IPersonService personService) : IRbacAdminService
{
    public async Task<IReadOnlyList<PermissionCatalogItemDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.roles.manage", cancellationToken: cancellationToken);
        return await db.Permissions.AsNoTracking()
            .OrderBy(p => p.BusinessArea).ThenBy(p => p.Module).ThenBy(p => p.SortOrder)
            .Select(p => new PermissionCatalogItemDto(
                p.Key, p.Module, p.Resource, p.Action, p.Label, p.Description,
                p.BusinessArea,
                ParseScopes(p.AllowedDataScopesJson),
                p.MenuPath))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.roles.manage", cancellationToken: cancellationToken);
        return await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id, r.Name, r.Slug, r.Description, r.BusinessArea,
                r.IsSystem, r.IsKeyUserTemplate, r.IsActive,
                r.RolePermissions.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDetailDto> GetRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.roles.manage", cancellationToken: cancellationToken);
        var role = await db.Roles.AsNoTracking()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Role não encontrada.");

        return MapRoleDetail(role);
    }

    public async Task<RoleDetailDto> CreateRoleAsync(UpsertRoleRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.roles.manage", cancellationToken: cancellationToken);
        var slug = Slugify(request.Name);
        var now = DateTimeOffset.UtcNow;
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim() ?? string.Empty,
            BusinessArea = request.BusinessArea,
            IsSystem = false,
            IsKeyUserTemplate = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return MapRoleDetail(role);
    }

    public async Task<RoleDetailDto> UpdateRoleAsync(Guid id, UpsertRoleRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.roles.manage", cancellationToken: cancellationToken);
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Role não encontrada.");
        if (role.IsSystem)
        {
            throw new InvalidOperationException("Roles de sistema não podem ser renomeadas.");
        }

        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim() ?? string.Empty;
        role.BusinessArea = request.BusinessArea;
        role.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await GetRoleAsync(id, cancellationToken);
    }

    public async Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.roles.manage", cancellationToken: cancellationToken);
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Role não encontrada.");
        if (role.IsSystem)
        {
            throw new InvalidOperationException("Roles de sistema não podem ser excluídas.");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RoleDetailDto> UpdateRolePermissionsAsync(Guid id, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.roles.manage", cancellationToken: cancellationToken);
        var role = await db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Role não encontrada.");

        db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var perm in request.Permissions.DistinctBy(p => p.PermissionKey))
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionKey = perm.PermissionKey,
                DataScope = perm.DataScope,
            });
        }

        role.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return MapRoleDetail(role);
    }

    public async Task<IReadOnlyList<SubjectRoleAssignmentDto>> GetAssignmentsAsync(RbacSubjectType? subjectType, string? query, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.assignments.manage", cancellationToken: cancellationToken);
        var q = db.SubjectRoleAssignments.AsNoTracking().Include(a => a.Role).AsQueryable();
        if (subjectType.HasValue)
        {
            q = q.Where(a => a.SubjectType == subjectType.Value);
        }

        var assignments = await q.OrderByDescending(a => a.AssignedAt).Take(500).ToListAsync(cancellationToken);
        var result = new List<SubjectRoleAssignmentDto>();

        foreach (var assignment in assignments)
        {
            var label = await ResolveSubjectLabelAsync(assignment.SubjectType, assignment.SubjectId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(query)
                && !label.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new SubjectRoleAssignmentDto(
                assignment.Id,
                assignment.SubjectType,
                assignment.SubjectId,
                label,
                assignment.RoleId,
                assignment.Role.Name,
                assignment.AssignedAt));
        }

        return result;
    }

    public async Task UpdateAssignmentsAsync(UpdateSubjectAssignmentsRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.assignments.manage", cancellationToken: cancellationToken);
        await ApplyAssignmentsAsync(request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task BulkUpdateAssignmentsAsync(BulkUpdateSubjectAssignmentsRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.assignments.manage", cancellationToken: cancellationToken);
        if (request.Items.Count == 0)
        {
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var item in request.Items)
            {
                await ApplyAssignmentsAsync(item, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<RbacSubjectSearchResultDto>> SearchSubjectsAsync(
        RbacSubjectType subjectType,
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.assignments.manage", cancellationToken: cancellationToken);
        var term = query.Trim();
        if (term.Length < 2)
        {
            return [];
        }

        var cappedLimit = Math.Clamp(limit, 1, 20);
        return subjectType switch
        {
            RbacSubjectType.Person => await SearchPeopleSubjectsAsync(term, cappedLimit, cancellationToken),
            RbacSubjectType.TestUser => await SearchTestUserSubjectsAsync(term, cappedLimit, cancellationToken),
            RbacSubjectType.PortalUser => await SearchPortalUserSubjectsAsync(term, cappedLimit, cancellationToken),
            _ => [],
        };
    }

    private async Task<IReadOnlyList<RbacSubjectSearchResultDto>> SearchPeopleSubjectsAsync(
        string term,
        int limit,
        CancellationToken cancellationToken)
    {
        var people = await personService.SearchAsync(term, limit, cancellationToken);
        return people.Select(person => new RbacSubjectSearchResultDto(
            RbacSubjectType.Person,
            person.Id,
            person.Name,
            person.DepartmentName)).ToList();
    }

    private async Task<IReadOnlyList<RbacSubjectSearchResultDto>> SearchTestUserSubjectsAsync(
        string term,
        int limit,
        CancellationToken cancellationToken)
    {
        return await db.TestUsers.AsNoTracking()
            .Where(user => user.IsActive
                && (user.Email.Contains(term) || user.DisplayName.Contains(term)))
            .OrderBy(user => user.DisplayName)
            .Take(limit)
            .Select(user => new RbacSubjectSearchResultDto(
                RbacSubjectType.TestUser,
                user.Id,
                user.DisplayName,
                user.Email))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RbacSubjectSearchResultDto>> SearchPortalUserSubjectsAsync(
        string term,
        int limit,
        CancellationToken cancellationToken)
    {
        return await db.PortalUsers.AsNoTracking()
            .Where(user => user.IsActive && user.Email.Contains(term))
            .OrderBy(user => user.Email)
            .Take(limit)
            .Select(user => new RbacSubjectSearchResultDto(
                RbacSubjectType.PortalUser,
                user.Id,
                user.Email,
                null))
            .ToListAsync(cancellationToken);
    }

    private async Task ApplyAssignmentsAsync(
        UpdateSubjectAssignmentsRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.SubjectRoleAssignments
            .Where(a => a.SubjectType == request.SubjectType && a.SubjectId == request.SubjectId)
            .ToListAsync(cancellationToken);

        db.SubjectRoleAssignments.RemoveRange(existing);
        var now = DateTimeOffset.UtcNow;
        Guid? assignedBy = null;
        try
        {
            assignedBy = await currentUserService.GetPersonIdAsync(cancellationToken);
        }
        catch
        {
            // ignore
        }

        foreach (var roleId in request.RoleIds.Distinct())
        {
            db.SubjectRoleAssignments.Add(new SubjectRoleAssignment
            {
                Id = Guid.NewGuid(),
                SubjectType = request.SubjectType,
                SubjectId = request.SubjectId,
                RoleId = roleId,
                AssignedByPersonId = assignedBy,
                AssignedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    public async Task<IReadOnlyList<TestUserDto>> GetTestUsersAsync(CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.test_users.manage", cancellationToken: cancellationToken);
        var users = await db.TestUsers.AsNoTracking().OrderBy(t => t.DisplayName).ToListAsync(cancellationToken);
        var result = new List<TestUserDto>();
        foreach (var user in users)
        {
            var roleNames = await GetSubjectRoleNamesAsync(RbacSubjectType.TestUser, user.Id, cancellationToken);
            result.Add(new TestUserDto(
                user.Id, user.Email, user.DisplayName, user.BusinessArea,
                user.OptionalPersonId, user.IsActive, user.ExpiresAt, user.Notes, roleNames));
        }

        return result;
    }

    public async Task<TestUserDto> CreateTestUserAsync(CreateTestUserRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.test_users.manage", cancellationToken: cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.TestUsers.AnyAsync(t => t.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("E-mail de test user já cadastrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var personId = await TestUserPersonProvisioning.ResolvePersonIdForNewTestUserAsync(
            db,
            email,
            request.DisplayName.Trim(),
            request.OptionalPersonId,
            cancellationToken);
        var user = new TestUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName.Trim(),
            BusinessArea = request.BusinessArea,
            OptionalPersonId = personId,
            IsActive = true,
            ExpiresAt = request.ExpiresAt ?? now.AddDays(90),
            Notes = request.Notes,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.TestUsers.Add(user);

        var roleIds = new List<Guid>();
        if (request.TemplateRoleId.HasValue)
        {
            roleIds.Add(request.TemplateRoleId.Value);
        }
        else
        {
            var templateSlug = request.BusinessArea switch
            {
                BusinessArea.RH => "KeyUser-RH",
                BusinessArea.TI => "KeyUser-TI",
                BusinessArea.Facilities => "KeyUser-Facilities",
                BusinessArea.UniLio => "KeyUser-UniLio-Instrutor",
                _ => "Employee",
            };
            var templateId = await db.Roles.AsNoTracking()
                .Where(r => r.Slug == templateSlug)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (templateId != Guid.Empty)
            {
                roleIds.Add(templateId);
            }
        }

        var employeeId = await db.Roles.AsNoTracking().Where(r => r.Slug == "Employee").Select(r => r.Id).FirstOrDefaultAsync(cancellationToken);
        if (employeeId != Guid.Empty)
        {
            roleIds.Add(employeeId);
        }

        foreach (var roleId in roleIds.Distinct())
        {
            db.SubjectRoleAssignments.Add(new SubjectRoleAssignment
            {
                Id = Guid.NewGuid(),
                SubjectType = RbacSubjectType.TestUser,
                SubjectId = user.Id,
                RoleId = roleId,
                AssignedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        var roleNames = await GetSubjectRoleNamesAsync(RbacSubjectType.TestUser, user.Id, cancellationToken);
        return new TestUserDto(user.Id, user.Email, user.DisplayName, user.BusinessArea, user.OptionalPersonId, user.IsActive, user.ExpiresAt, user.Notes, roleNames);
    }

    public async Task<TestUserDto> UpdateTestUserAsync(Guid id, UpdateTestUserRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.test_users.manage", cancellationToken: cancellationToken);
        var user = await db.TestUsers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Test user não encontrado.");
        user.DisplayName = request.DisplayName.Trim();
        user.BusinessArea = request.BusinessArea;
        user.OptionalPersonId = request.OptionalPersonId;
        user.IsActive = request.IsActive;
        user.ExpiresAt = request.ExpiresAt;
        user.Notes = request.Notes;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var roleNames = await GetSubjectRoleNamesAsync(RbacSubjectType.TestUser, user.Id, cancellationToken);
        return new TestUserDto(user.Id, user.Email, user.DisplayName, user.BusinessArea, user.OptionalPersonId, user.IsActive, user.ExpiresAt, user.Notes, roleNames);
    }

    public async Task DeleteTestUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.test_users.manage", cancellationToken: cancellationToken);
        var user = await db.TestUsers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Test user não encontrado.");
        db.TestUsers.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetTestUserPasswordAsync(Guid id, ResetTestUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("rbac.test_users.manage", cancellationToken: cancellationToken);
        var user = await db.TestUsers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Test user não encontrado.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetSubjectRoleNamesAsync(RbacSubjectType subjectType, Guid subjectId, CancellationToken cancellationToken) =>
        await db.SubjectRoleAssignments.AsNoTracking()
            .Where(a => a.SubjectType == subjectType && a.SubjectId == subjectId)
            .Join(db.Roles.AsNoTracking(), a => a.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);

    private async Task<string> ResolveSubjectLabelAsync(RbacSubjectType subjectType, Guid subjectId, CancellationToken cancellationToken) =>
        subjectType switch
        {
            RbacSubjectType.Person => await db.People.AsNoTracking().Where(p => p.Id == subjectId).Select(p => p.Email).FirstOrDefaultAsync(cancellationToken) ?? subjectId.ToString(),
            RbacSubjectType.PortalUser => await db.PortalUsers.AsNoTracking().Where(p => p.Id == subjectId).Select(p => p.Email).FirstOrDefaultAsync(cancellationToken) ?? subjectId.ToString(),
            RbacSubjectType.TestUser => await db.TestUsers.AsNoTracking().Where(t => t.Id == subjectId).Select(t => t.Email).FirstOrDefaultAsync(cancellationToken) ?? subjectId.ToString(),
            _ => subjectId.ToString(),
        };

    private static RoleDetailDto MapRoleDetail(Role role) =>
        new(role.Id, role.Name, role.Slug, role.Description, role.BusinessArea, role.IsSystem, role.IsKeyUserTemplate, role.IsActive,
            role.RolePermissions.Select(rp => new RolePermissionDto(rp.PermissionKey, rp.DataScope)).ToList());

    private static IReadOnlyList<DataScope> ParseScopes(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json)?
                       .Select(s => Enum.TryParse<DataScope>(s, true, out var scope) ? scope : DataScope.Self)
                       .ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string Slugify(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
