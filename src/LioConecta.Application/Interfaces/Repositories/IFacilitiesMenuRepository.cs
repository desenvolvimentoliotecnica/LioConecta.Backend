using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IFacilitiesMenuRepository
{
    Task<CafeteriaMenu?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CafeteriaMenu>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<CafeteriaMenu> UpsertAsync(CafeteriaMenu menu, CancellationToken cancellationToken = default);

    Task DeleteAsync(DateOnly date, CancellationToken cancellationToken = default);
}
