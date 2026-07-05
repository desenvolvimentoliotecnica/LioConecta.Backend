using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class ServiceRequestEvent : BaseEntity
{
    public Guid ServiceRequestId { get; set; }

    public ServiceRequest? ServiceRequest { get; set; }

    public string EventType { get; set; } = string.Empty;

    public Guid? ActorId { get; set; }

    public Person? Actor { get; set; }

    public string? DetailsJson { get; set; }
}
