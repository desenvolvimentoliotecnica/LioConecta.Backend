using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? Href { get; set; }

    public bool IsRead { get; set; }
}
