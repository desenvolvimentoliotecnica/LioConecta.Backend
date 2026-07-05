using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class AuditEvent : BaseEntity
{
    public Guid CorrelationId { get; set; }

    public Guid TransactionId { get; set; }

    public AuditSource Source { get; set; }

    public string Action { get; set; } = string.Empty;

    public Guid? ActorId { get; set; }

    public Person? Actor { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string? HttpMethod { get; set; }

    public string? Path { get; set; }

    public int? StatusCode { get; set; }

    public int? DurationMs { get; set; }

    public string? DetailsJson { get; set; }
}
