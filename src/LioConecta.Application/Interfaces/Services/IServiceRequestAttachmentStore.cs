using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public static class ServiceRequestAttachmentLimits
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const int MaxFilesPerMessage = 3;
}

public interface IServiceRequestAttachmentStore
{
    Task<ServiceRequestAttachmentMetaDto> SaveAsync(
        Stream content,
        string fileName,
        string? contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    string? ResolveAbsolutePath(string storageFileName);
}
