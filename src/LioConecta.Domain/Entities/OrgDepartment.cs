using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class OrgDepartment : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? ParentDepartmentId { get; set; }

    public OrgDepartment? ParentDepartment { get; set; }

    public ICollection<OrgDepartment> ChildDepartments { get; set; } = [];

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
