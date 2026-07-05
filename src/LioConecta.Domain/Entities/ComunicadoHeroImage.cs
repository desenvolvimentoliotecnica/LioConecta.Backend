using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class ComunicadoHeroImage : BaseEntity
{
    public Guid AssetId { get; set; }

    public int Version { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public string PublicUrl { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public Guid UploadedById { get; set; }

    public Person? UploadedBy { get; set; }
}
