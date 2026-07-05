namespace LioConecta.Domain.Entities;

public class ObservabilityEvent : Common.BaseEntity
{
    public DateTimeOffset OccurredAt { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public short Severity { get; set; } = 2;

    public string Application { get; set; } = "LioConecta.Api";

    public string Environment { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public Person? User { get; set; }

    public Guid? SessionId { get; set; }

    public Guid CorrelationId { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? RequestId { get; set; }

    public string? HttpMethod { get; set; }

    public string? Route { get; set; }

    public string? RouteTemplate { get; set; }

    public int? StatusCode { get; set; }

    public int? DurationMs { get; set; }

    public string? ResourceType { get; set; }

    public string? ResourceId { get; set; }

    public string? Action { get; set; }

    public bool Success { get; set; } = true;

    public string? ErrorType { get; set; }

    public string? ErrorCode { get; set; }

    public string? MetadataJson { get; set; }
}
