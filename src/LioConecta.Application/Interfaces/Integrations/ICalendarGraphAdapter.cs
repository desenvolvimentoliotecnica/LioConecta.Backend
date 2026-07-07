using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ICalendarGraphAdapter
{
    Task<IReadOnlyList<GraphCalendarListItem>> ListCalendarsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraphCalendarEventDetail>> GetCalendarViewAsync(
        string accessToken,
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<string>? calendarIds,
        CancellationToken cancellationToken = default);

    Task<GraphCalendarEventDetail> CreateEventAsync(
        string accessToken,
        string calendarId,
        GraphCalendarEventWrite write,
        CancellationToken cancellationToken = default);

    Task<GraphCalendarEventDetail> UpdateEventAsync(
        string accessToken,
        string eventId,
        GraphCalendarEventWrite write,
        CancellationToken cancellationToken = default);

    Task DeleteEventAsync(
        string accessToken,
        string eventId,
        CancellationToken cancellationToken = default);
}
