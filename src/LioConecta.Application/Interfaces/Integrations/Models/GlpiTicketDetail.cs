namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GlpiTicketDetail
{
    public GlpiTicketSummary Summary { get; set; } = new();

    public string Description { get; set; } = string.Empty;

    public string? Assignee { get; set; }

    public IReadOnlyList<GlpiTicketFollowup> Followups { get; set; } = [];
}

public sealed class GlpiTicketFollowup
{
    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? Author { get; set; }
}
