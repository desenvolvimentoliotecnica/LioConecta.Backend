namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GraphDocument
{
    public string ItemId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string WebUrl { get; set; } = string.Empty;

    public DateTimeOffset ModifiedAt { get; set; }
}
