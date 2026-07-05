using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> ListAsync(string? category, CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
