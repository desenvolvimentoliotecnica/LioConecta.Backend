using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class RbacTokenResolver(AppDbContext db)
{
    public async Task<IReadOnlyList<EffectivePermissionDto>> ResolvePermissionsAsync(
        RbacSubjectType subjectType,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        var roleIds = await db.SubjectRoleAssignments.AsNoTracking()
            .Where(a => a.SubjectType == subjectType && a.SubjectId == subjectId)
            .Select(a => a.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0 && subjectType == RbacSubjectType.Person)
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
            .Select(rp => new EffectivePermissionDto(rp.PermissionKey, rp.DataScope))
            .ToListAsync(cancellationToken);

        return PermissionService.MergePermissions(rolePermissions);
    }
}
