using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ICalendarService
{
    Task<CalendarBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);

    Task<CalendarStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task LinkAccountAsync(LinkCalendarAccountRequest request, CancellationToken cancellationToken = default);

    Task UnlinkAccountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarListItemDto>> GetCalendarsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarEventDto>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<string>? calendarIds,
        CancellationToken cancellationToken = default);

    Task<CalendarEventDto> CreateEventAsync(
        CreateCalendarEventRequest request,
        CancellationToken cancellationToken = default);

    Task<CalendarEventDto> UpdateEventAsync(
        string eventId,
        UpdateCalendarEventRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteEventAsync(string eventId, CancellationToken cancellationToken = default);

    Task<DailyMenuDto?> GetCafeteriaMenuAsync(DateOnly date, CancellationToken cancellationToken = default);
}
