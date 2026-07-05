using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface ICalendarRepository
{
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<CafeteriaMenu?> GetCafeteriaMenuAsync(DateOnly date, CancellationToken cancellationToken = default);
}
