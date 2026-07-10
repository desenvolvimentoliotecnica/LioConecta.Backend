using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPontoAttachmentStore
{
    Task<PontoAttachmentMetaDto> SaveAsync(
        Stream content,
        string fileName,
        string? contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    string? ResolveAbsolutePath(string storageFileName);
}
