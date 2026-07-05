using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IMoodCheckRepository
{
    Task<MoodCheck?> GetByPersonAndDateAsync(
        Guid personId,
        DateOnly checkDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(MoodCheck moodCheck, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoodCheck>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
