using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Services;

public sealed class DocumentService(IDocumentRepository documentRepository) : IDocumentService
{
    public async Task<IReadOnlyList<DocumentDto>> ListAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        var documents = await documentRepository.ListAsync(category, cancellationToken);
        return documents.Select(DocumentMapper.ToDto).ToList();
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(id, cancellationToken);
        return document is null ? null : DocumentMapper.ToDto(document);
    }
}
