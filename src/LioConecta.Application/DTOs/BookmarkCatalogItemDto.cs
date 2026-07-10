namespace LioConecta.Application.DTOs;

public sealed record BookmarkCatalogItemDto(
    Guid Id,
    string SeedKey,
    string Kind,
    string Title,
    string Excerpt,
    string Href,
    string Icon,
    string Source,
    bool IsDefault,
    int SortOrder);
