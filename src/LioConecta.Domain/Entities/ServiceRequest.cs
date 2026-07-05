using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class ServiceRequest : BaseEntity
{
    public string Type { get; set; } = string.Empty;

    public ServiceCategory Category { get; set; }

    public ServiceRequestStatus Status { get; set; }

    public Guid RequesterId { get; set; }

    public Person? Requester { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public string? AssigneeTeam { get; set; }

    public string? ExternalRef { get; set; }

    public string TimelineJson { get; set; } = "[]";

    public ICollection<ServiceRequestEvent> Events { get; set; } = [];
}
