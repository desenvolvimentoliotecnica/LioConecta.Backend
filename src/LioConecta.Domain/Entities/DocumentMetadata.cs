using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class DocumentMetadata : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string SharePointUrl { get; set; } = string.Empty;

    public string SharePointItemId { get; set; } = string.Empty;

    public DateTimeOffset ModifiedAt { get; set; }
}
