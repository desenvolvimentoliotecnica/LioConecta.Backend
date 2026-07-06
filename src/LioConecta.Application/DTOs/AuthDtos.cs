namespace LioConecta.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresInSeconds,
    MeDto User);

public sealed record TestLdapConnectionRequest(
    string? Host = null,
    int? Port = null,
    bool? UseSsl = null,
    string? BindDn = null,
    string? BindPassword = null,
    string? SearchBase = null);

public sealed record LdapConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail);
