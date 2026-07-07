namespace LioConecta.Application.DTOs;

public sealed record LoopBootstrapDto(
    bool Enabled,
    IReadOnlyList<string> AllowedRoles,
    IReadOnlyList<string> AllowedEmails);
