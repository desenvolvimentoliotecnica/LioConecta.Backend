namespace LioConecta.Application.DTOs;

public sealed record CalendarEventDto(
    Guid Id,
    string Title,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Location,
    string Source);

public sealed record CafeteriaMenuDto(
    DateOnly Date,
    IReadOnlyList<string> Items);
