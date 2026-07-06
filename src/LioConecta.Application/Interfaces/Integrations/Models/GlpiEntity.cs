namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GlpiEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public int? ParentId { get; set; }

    public bool HasChildren { get; set; }
}
