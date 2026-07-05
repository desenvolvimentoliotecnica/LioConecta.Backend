using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Services;

public sealed class CalendarService(ICalendarRepository calendarRepository) : ICalendarService
{
    public async Task<IReadOnlyList<CalendarEventDto>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new ArgumentException("End date must be after start date.", nameof(to));
        }

        var events = await calendarRepository.GetEventsAsync(from, to, cancellationToken);
        return events.Select(CalendarMapper.ToDto).ToList();
    }

    public async Task<CafeteriaMenuDto?> GetCafeteriaMenuAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var menu = await calendarRepository.GetCafeteriaMenuAsync(date, cancellationToken);
        return menu is null ? null : CalendarMapper.ToDto(menu);
    }
}
