namespace LioConecta.Application.Interfaces.Services;

public interface IPersonPhotoStorageService
{
    string BuildPublicUrl(string slug);

    Task<string> SaveAsync(string slug, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
}
