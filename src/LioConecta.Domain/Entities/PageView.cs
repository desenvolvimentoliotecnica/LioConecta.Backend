namespace LioConecta.Domain.Entities;

public class PageView : Common.BaseEntity
{
    public DateTimeOffset OccurredAt { get; set; }

    public Guid? UserId { get; set; }

    public Person? User { get; set; }

    public Guid SessionId { get; set; }

    public Guid CorrelationId { get; set; }

    public string PageName { get; set; } = string.Empty;

    public string RouteTemplate { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string? ReferrerTemplate { get; set; }

    public int? DurationMs { get; set; }

    public string? MetadataJson { get; set; }
}
