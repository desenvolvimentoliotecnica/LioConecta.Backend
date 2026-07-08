namespace LioConecta.Application.DTOs;

public sealed record PhoneExtensionDto(
    Guid Id, string Name, string Extension, string? Mobile, string Department,
    string? Title, string? Email, string? ManagerName, Guid? PersonId,
    string? PersonSlug, string? PersonName, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record PhoneExtensionsBootstrapDto(bool CanManage, int Total, IReadOnlyList<string> Departments);

public sealed record UpsertPhoneExtensionRequest(
    string Name, string Extension, string? Mobile, string Department,
    string? Title, string? Email, string? ManagerName, Guid? PersonId, bool IsActive = true);

public sealed record PhoneExtensionManagePolicyDto(bool CanManage);