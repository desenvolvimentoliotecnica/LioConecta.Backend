namespace LioConecta.Domain.Entities;

public class DbExplorerDerLayout
{
    public Guid Id { get; set; }

    public Guid ActorId { get; set; }

    public string ConnectionId { get; set; } = string.Empty;

    public string LayoutJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Person? Actor { get; set; }
}
