namespace LioConecta.Application.DTOs;

public sealed record UserPreferencesDto(
    IReadOnlyList<string> Bookmarks,
    IReadOnlyList<string> Favorites,
    IReadOnlyList<string> Shortcuts);

public sealed record UpdatePreferencesRequest(
    IReadOnlyList<string>? Bookmarks,
    IReadOnlyList<string>? Favorites,
    IReadOnlyList<string>? Shortcuts);
