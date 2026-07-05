namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GlpiTicketResult
{
    public string TicketId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Url { get; set; }
}
