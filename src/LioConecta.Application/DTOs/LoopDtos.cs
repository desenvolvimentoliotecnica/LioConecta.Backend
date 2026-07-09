namespace LioConecta.Application.DTOs;

public sealed record LoopBootstrapDto(
    bool Enabled,
    bool CanAccess,
    IReadOnlyList<string> AllowedRoles,
    IReadOnlyList<string> AllowedEmails);
