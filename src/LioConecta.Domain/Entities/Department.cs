using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    public Department? ParentDepartment { get; set; }

    public ICollection<Department> ChildDepartments { get; set; } = [];

    public ICollection<Person> Members { get; set; } = [];
}
