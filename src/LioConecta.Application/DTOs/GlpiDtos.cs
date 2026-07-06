namespace LioConecta.Application.DTOs;

public sealed record TestGlpiConnectionRequest(
    string? BaseUrl,
    string? AppToken,
    string? UserToken);

public sealed record GlpiConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail,
    bool UsesDevAdapters);

public sealed record GlpiRuntimeCredentials(
    string BaseUrl,
    string AppToken,
    string UserToken,
    string PortalUrl,
    int? ProfileId = null);
