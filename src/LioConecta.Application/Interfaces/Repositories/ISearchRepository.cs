using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface ISearchRepository
{
    Task<IReadOnlyList<Person>> SearchPeopleAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentMetadata>> SearchDocumentsAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Comunicado>> SearchComunicadosAsync(string query, int limit, CancellationToken cancellationToken = default);
}
