using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task<IReadOnlyList<DocumentMetadata>> ListAsync(string? category, CancellationToken cancellationToken = default);

    Task<DocumentMetadata?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentMetadata>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task UpsertAsync(DocumentMetadata document, CancellationToken cancellationToken = default);
}
