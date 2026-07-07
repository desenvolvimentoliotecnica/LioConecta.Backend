using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class OrgChartSettings : BaseEntity
{
    public bool GovernanceEnabled { get; set; }

    public string EditAllowedRolesJson { get; set; } = "[]";

    public string EditAllowedEmailsJson { get; set; } = "[]";

    public string ViewFullAllowedRolesJson { get; set; } = "[]";

    public bool AllowDisplayNameEdit { get; set; }

    public bool AllowReimport { get; set; } = true;

    public bool ShowOverrideBadge { get; set; } = true;

    public DateTimeOffset? LastImportAt { get; set; }

    public Guid? UpdatedById { get; set; }

    public Person? UpdatedBy { get; set; }
}
