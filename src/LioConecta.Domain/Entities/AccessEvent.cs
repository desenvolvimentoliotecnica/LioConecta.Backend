namespace LioConecta.Domain.Entities;

public class AccessEvent : Common.BaseEntity
{
    public DateTimeOffset OccurredAt { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public Person? User { get; set; }

    public string? UsernameSnapshot { get; set; }

    public Guid? SessionId { get; set; }

    public Guid CorrelationId { get; set; }

    public string? Resource { get; set; }

    public string? Action { get; set; }

    public string? Permission { get; set; }

    public string Result { get; set; } = string.Empty;

    public string? ReasonCode { get; set; }

    public string? IpAddress { get; set; }

    public string? IpHash { get; set; }

    public string? UserAgent { get; set; }

    public string? MetadataJson { get; set; }
}
