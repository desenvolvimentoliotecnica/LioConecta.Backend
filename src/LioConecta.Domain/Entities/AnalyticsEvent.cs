using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class AnalyticsEvent : BaseEntity
{
    public string EventType { get; set; } = string.Empty;

    public Guid? PersonId { get; set; }

    public Person? Person { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset OccurredAt { get; set; }
}
