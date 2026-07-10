namespace LioConecta.Domain.Entities;

public class DbExplorerQueryLog
{
    public Guid Id { get; set; }

    public Guid ActorId { get; set; }

    public string ConnectionId { get; set; } = string.Empty;

    public string SqlText { get; set; } = string.Empty;

    public int RowCount { get; set; }

    public int DurationMs { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset ExecutedAt { get; set; }

    public Person? Actor { get; set; }
}
