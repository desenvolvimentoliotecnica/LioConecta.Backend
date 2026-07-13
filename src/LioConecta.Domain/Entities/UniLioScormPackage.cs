using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioScormPackage : BaseEntity
{
    public Guid CourseId { get; set; }

    public Guid ModuleId { get; set; }

    public string Version { get; set; } = "1.2";

    public string ManifestTitle { get; set; } = string.Empty;

    public string LaunchPath { get; set; } = string.Empty;

    public string StorageRoot { get; set; } = string.Empty;

    public int ScoCount { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    public UniLioCourse Course { get; set; } = null!;

    public UniLioCourseModule Module { get; set; } = null!;
}
