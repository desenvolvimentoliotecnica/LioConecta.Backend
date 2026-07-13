namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GlpiTicketDetail
{
    public GlpiTicketSummary Summary { get; set; } = new();

    public string Description { get; set; } = string.Empty;

    public string? Assignee { get; set; }

    public IReadOnlyList<GlpiTicketFollowup> Followups { get; set; } = [];

    public IReadOnlyList<GlpiTicketAttachment> Attachments { get; set; } = [];
}

public sealed class GlpiTicketFollowup
{
    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? Author { get; set; }
}

public sealed class GlpiTicketAttachment
{
    public string DocumentId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }
}

public sealed class GlpiTicketAttachmentContent
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public byte[] Content { get; set; } = [];
}
