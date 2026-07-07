namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GraphCalendarEventDetail
{
    public string Id { get; set; } = string.Empty;

    public string CalendarId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public bool IsAllDay { get; set; }

    public string? Location { get; set; }

    public string? Description { get; set; }

    public string? OnlineMeetingUrl { get; set; }

    public string? WebLink { get; set; }

    public string? OrganizerName { get; set; }

    public string? OrganizerEmail { get; set; }

    public string? Color { get; set; }

    public bool CanEdit { get; set; } = true;
}

public sealed class GraphCalendarEventWrite
{
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public bool IsAllDay { get; set; }

    public string? Location { get; set; }

    public string? Description { get; set; }
}
