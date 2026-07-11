using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class Comunicado : BaseEntity
{
    public ComunicadoKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? Excerpt { get; set; }

    public string ContentJson { get; set; } = "{}";

    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public string? HeroImageUrl { get; set; }

    public bool IsMandatory { get; set; }

    public ComunicadoStatus Status { get; set; } = ComunicadoStatus.Published;

    public DateTimeOffset? ScheduledAt { get; set; }

    public ComunicadoAudienceType AudienceType { get; set; } = ComunicadoAudienceType.All;

    public string AudienceDepartmentIdsJson { get; set; } = "[]";

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public ICollection<ComunicadoRead> Reads { get; set; } = [];
}
