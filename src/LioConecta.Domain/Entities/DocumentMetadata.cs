using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class DocumentMetadata : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? MediaType { get; set; }

    public bool IsFeatured { get; set; }

    public string? SeedKey { get; set; }

    public string SharePointUrl { get; set; } = string.Empty;

    public string SharePointItemId { get; set; } = string.Empty;

    public DateTimeOffset ModifiedAt { get; set; }
}
