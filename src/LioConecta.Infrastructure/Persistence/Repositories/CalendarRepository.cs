using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class CalendarRepository(AppDbContext db) : ICalendarRepository
{
    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        db.CalendarEvents
            .AsNoTracking()
            .Where(e => e.StartAt >= from && e.StartAt <= to)
            .OrderBy(e => e.StartAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CalendarEvent>)t.Result, cancellationToken);

    public Task<CafeteriaMenu?> GetCafeteriaMenuAsync(
        DateOnly date,
        CancellationToken cancellationToken = default) =>
        db.CafeteriaMenus
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Date == date, cancellationToken);
}
