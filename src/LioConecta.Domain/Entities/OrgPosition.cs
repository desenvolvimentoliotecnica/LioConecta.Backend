using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class OrgPosition : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public string? Title { get; set; }

    public string? DepartmentName { get; set; }

    public Guid? OrgDepartmentId { get; set; }

    public OrgDepartment? OrgDepartment { get; set; }

    public Guid? ManagerPositionId { get; set; }

    public OrgPosition? ManagerPosition { get; set; }

    public ICollection<OrgPosition> DirectReports { get; set; } = [];

    public bool IsVisible { get; set; } = true;

    public int SortOrder { get; set; }

    public bool HasManualOverride { get; set; }

    public OrgPositionSource Source { get; set; } = OrgPositionSource.Graph;
}
