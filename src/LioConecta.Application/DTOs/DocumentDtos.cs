namespace LioConecta.Application.DTOs;

public sealed record DocumentDto(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string? MediaType,
    bool IsFeatured,
    string? SeedKey,
    string SharePointUrl,
    DateTimeOffset ModifiedAt);
