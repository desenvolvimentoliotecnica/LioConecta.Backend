using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioModuleAttachment : BaseEntity
{
    public Guid ModuleId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StorageFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int SortOrder { get; set; }

    public UniLioCourseModule Module { get; set; } = null!;
}
