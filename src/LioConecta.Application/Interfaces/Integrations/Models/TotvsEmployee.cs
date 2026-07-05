namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class TotvsEmployee
{
    public string ExternalId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? DepartmentCode { get; set; }

    public string? ManagerExternalId { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? HireDate { get; set; }

    public bool IsActive { get; set; } = true;
}
