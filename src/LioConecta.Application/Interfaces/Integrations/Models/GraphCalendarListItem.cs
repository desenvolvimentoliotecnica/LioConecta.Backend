namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GraphCalendarListItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public bool CanEdit { get; set; }

    public bool IsDefaultCalendar { get; set; }
}
