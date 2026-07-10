namespace LioConecta.Domain.Entities;

public class DbExplorerSavedQuery
{
    public Guid Id { get; set; }

    public Guid ActorId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ConnectionId { get; set; } = string.Empty;

    public string SqlText { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Person? Actor { get; set; }
}
