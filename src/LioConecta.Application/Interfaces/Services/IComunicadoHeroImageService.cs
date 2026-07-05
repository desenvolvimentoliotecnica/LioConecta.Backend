using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public sealed record ComunicadoHeroUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid? AssetId);

public interface IComunicadoHeroImageService
{
    IReadOnlyList<ComunicadoHeroTemplateDto> GetTemplates();

    Task<IReadOnlyList<ComunicadoHeroUploadDto>> GetRecentUploadsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<UploadComunicadoHeroResponseDto> UploadAsync(
        ComunicadoHeroUploadRequest request,
        Guid uploadedById,
        CancellationToken cancellationToken = default);
}
