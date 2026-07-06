using LioConecta.Application.Common;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Person?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetOrgChartPeopleAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetDirectoryPeopleAsync(
        string? query,
        string? departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetPeersAsync(
        Guid personId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetDirectReportsAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Person person, CancellationToken cancellationToken = default);

    Task UpdateAsync(Person person, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetByAzureObjectIdsAsync(
        IEnumerable<Guid> objectIds,
        CancellationToken cancellationToken = default);
}
