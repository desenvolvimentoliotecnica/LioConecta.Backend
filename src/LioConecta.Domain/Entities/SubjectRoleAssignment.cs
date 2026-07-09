using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class SubjectRoleAssignment : BaseEntity
{
    public RbacSubjectType SubjectType { get; set; }

    public Guid SubjectId { get; set; }

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Guid? AssignedByPersonId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }
}
