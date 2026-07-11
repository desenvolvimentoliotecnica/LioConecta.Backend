using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class MoodCheckRepository(AppDbContext db) : IMoodCheckRepository
{
    public Task<MoodCheck?> GetByPersonAndDateAsync(
        Guid personId,
        DateOnly checkDate,
        CancellationToken cancellationToken = default) =>
        db.MoodChecks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.PersonId == personId && m.CheckDate == checkDate,
                cancellationToken);

    public async Task AddAsync(MoodCheck moodCheck, CancellationToken cancellationToken = default)
    {
        db.MoodChecks.Add(moodCheck);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MoodCheck>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        await db.MoodChecks
            .AsNoTracking()
            .Include(m => m.Person)
                .ThenInclude(p => p!.Department)
            .Where(m => m.CheckDate >= from && m.CheckDate <= to)
            .OrderByDescending(m => m.CheckDate)
            .ThenByDescending(m => m.RecordedAt)
            .ToListAsync(cancellationToken);
}
