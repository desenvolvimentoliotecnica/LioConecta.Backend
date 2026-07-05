using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class AuditEvent : BaseEntity
{
    public string Action { get; set; } = string.Empty;

    public Guid? ActorId { get; set; }

    public Person? Actor { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string? DetailsJson { get; set; }
}
