namespace LioConecta.Application.Common.Audit;

public sealed class AuditContext
{
    public const string HttpContextItemKey = "LioConecta.AuditContext";
    public const string CorrelationHeaderName = "X-Correlation-Id";

    public Guid CorrelationId { get; init; }

    public Guid TransactionId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public string? HttpMethod { get; set; }

    public string? Path { get; set; }

    public Guid? ActorId { get; set; }

    public bool SuppressChangeAudit { get; set; }

    public List<PendingAuditEvent> PendingEvents { get; } = [];
}

public sealed class PendingAuditEvent
{
    public required string Action { get; init; }

    public required string TargetType { get; init; }

    public required string TargetId { get; init; }

    public required Domain.Enums.AuditSource Source { get; init; }

    public Guid? ActorId { get; init; }

    public string? HttpMethod { get; init; }

    public string? Path { get; init; }

    public int? StatusCode { get; init; }

    public int? DurationMs { get; init; }

    public string? DetailsJson { get; init; }
}
