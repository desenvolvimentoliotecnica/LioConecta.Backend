using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class Permission
{
    public string Key { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Resource { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public BusinessArea BusinessArea { get; set; }

    public string AllowedDataScopesJson { get; set; } = "[]";

    public string? MenuPath { get; set; }

    public bool IsSystem { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
