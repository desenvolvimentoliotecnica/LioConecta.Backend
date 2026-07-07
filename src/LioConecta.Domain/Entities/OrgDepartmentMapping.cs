using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

/// <summary>
/// Maps a directory/Graph department name (de) to a governed <see cref="OrgDepartment"/> (para).
/// </summary>
public class OrgDepartmentMapping : BaseEntity
{
    public string SourceName { get; set; } = string.Empty;

    public Guid? OrgDepartmentId { get; set; }

    public OrgDepartment? OrgDepartment { get; set; }

    public bool IsActive { get; set; } = true;
}
