using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarEventDto>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<CafeteriaMenuDto?> GetCafeteriaMenuAsync(DateOnly date, CancellationToken cancellationToken = default);
}
