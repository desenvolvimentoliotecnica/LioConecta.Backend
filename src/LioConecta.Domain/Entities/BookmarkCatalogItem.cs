using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class BookmarkCatalogItem : BaseEntity
{
    public string SeedKey { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Excerpt { get; set; } = string.Empty;

    public string Href { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
