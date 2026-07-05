namespace LioConecta.Application.DTOs;

public sealed record DocumentDto(
    Guid Id,
    string Title,
    string Category,
    string SharePointUrl,
    DateTimeOffset ModifiedAt);
