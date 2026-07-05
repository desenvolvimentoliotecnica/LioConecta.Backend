using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class CalendarEvent : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string? Location { get; set; }

    public string Source { get; set; } = "Internal";

    public string? ExternalId { get; set; }
}
