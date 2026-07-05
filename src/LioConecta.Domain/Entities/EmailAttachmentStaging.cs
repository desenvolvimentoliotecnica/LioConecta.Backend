using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class EmailAttachmentStaging : BaseEntity
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public Guid CreatedById { get; set; }

    public Person? CreatedBy { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsConsumed { get; set; }
}
