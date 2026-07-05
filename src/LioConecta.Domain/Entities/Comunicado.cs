using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class Comunicado : BaseEntity
{
    public ComunicadoKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Excerpt { get; set; }

    public string ContentJson { get; set; } = "{}";

    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public string? HeroImageUrl { get; set; }

    public bool IsMandatory { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public ICollection<ComunicadoRead> Reads { get; set; } = [];
}
