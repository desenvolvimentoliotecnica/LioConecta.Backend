using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public BusinessArea? BusinessArea { get; set; }

    public bool IsSystem { get; set; }

    public bool IsKeyUserTemplate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];

    public ICollection<SubjectRoleAssignment> SubjectAssignments { get; set; } = [];
}
