namespace LioConecta.Application.DTOs;

public sealed record TestGraphConnectionRequest(
    string? TenantId,
    string? ClientId,
    string? ClientSecret);

public sealed record GraphConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail,
    bool UsesDevAdapters,
    int? DomainUserCount,
    int? TenantUserCount);

public sealed record GraphRuntimeCredentials(
    string TenantId,
    string ClientId,
    string ClientSecret,
    string EmailDomain = "liotecnica.com.br");
