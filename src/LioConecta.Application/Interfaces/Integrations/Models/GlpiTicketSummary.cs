namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GlpiTicketSummary
{
    public string TicketId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public string PriorityLabel { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? Url { get; set; }

    public string? RequesterLabel { get; set; }
}
