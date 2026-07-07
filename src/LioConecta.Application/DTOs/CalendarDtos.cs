namespace LioConecta.Application.DTOs;

public sealed record CalendarBootstrapDto(
    bool Enabled,
    IReadOnlyList<string> DelegatedScopes,
    string DefaultView,
    bool ShowBirthdays,
    bool ShowCafeteriaMenu,
    string MsalClientId,
    string MsalTenantId,
    string MsalAuthority);

public sealed record CalendarStatusDto(
    bool Enabled,
    bool Linked,
    bool NeedsConsent);

public sealed record CalendarListItemDto(
    string Id,
    string Name,
    string? Color,
    bool CanEdit,
    bool IsDefaultCalendar);

public sealed record CalendarEventDto(
    string GraphId,
    string CalendarId,
    string Title,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool IsAllDay,
    string? Location,
    string? Description,
    string? OnlineMeetingUrl,
    string? WebLink,
    string? OrganizerName,
    string? OrganizerEmail,
    string Source,
    string? Color,
    bool CanEdit);

public sealed record CafeteriaMenuDto(
    DateOnly Date,
    IReadOnlyList<string> Items);

public sealed record LinkCalendarAccountRequest(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<string>? Scopes);

public sealed record CreateCalendarEventRequest(
    string CalendarId,
    string Title,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    bool IsAllDay,
    string? Location,
    string? Description);

public sealed record UpdateCalendarEventRequest(
    string? Title,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    bool? IsAllDay,
    string? Location,
    string? Description);

public sealed record TestCalendarConnectionRequest(
    string? TokenEncryptionKey);

public sealed record CalendarConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail,
    bool CalendarEnabled,
    string? TenantId,
    string? ClientId);
