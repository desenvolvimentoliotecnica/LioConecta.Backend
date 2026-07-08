using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class PortalSystem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public PortalSystemDestinationType DestinationType { get; set; } = PortalSystemDestinationType.External;
    public string? UrlDev { get; set; }
    public string? UrlHml { get; set; }
    public string? UrlPrd { get; set; }
    public PortalSystemIconKind IconKind { get; set; } = PortalSystemIconKind.FontAwesome;
    public string? IconFaClass { get; set; }
    public string? IconAssetUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AccessNotes { get; set; }
    public long ClickCount { get; set; }
    public string? SeedKey { get; set; }
}
