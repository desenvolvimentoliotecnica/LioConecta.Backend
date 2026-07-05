namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GraphDirectoryUser
{
    public Guid ObjectId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? UserPrincipalName { get; init; }

    public string? Mail { get; init; }

    public string? JobTitle { get; init; }

    public string? Department { get; init; }

    public string? MobilePhone { get; init; }

    public IReadOnlyList<string> BusinessPhones { get; init; } = [];

    public string? OfficeLocation { get; init; }

    public string? EmployeeId { get; init; }

    public bool AccountEnabled { get; init; } = true;

    public Guid? ManagerObjectId { get; init; }
}
