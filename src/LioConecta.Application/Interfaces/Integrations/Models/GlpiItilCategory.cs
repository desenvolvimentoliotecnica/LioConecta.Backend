namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GlpiItilCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? FullName { get; set; }
}
