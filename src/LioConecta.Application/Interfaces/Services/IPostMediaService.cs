using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public sealed record PostMediaUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);

public interface IPostMediaService
{
    Task<UploadPostMediaResponseDto> UploadAsync(
        PostMediaUploadRequest request,
        Guid uploadedById,
        CancellationToken cancellationToken = default);
}
