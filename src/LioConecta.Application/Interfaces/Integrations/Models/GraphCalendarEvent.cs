namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GraphCalendarEvent
{
    public string ExternalId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public string? Location { get; set; }
}
