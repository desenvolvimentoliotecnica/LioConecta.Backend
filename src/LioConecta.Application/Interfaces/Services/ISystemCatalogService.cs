using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ISystemCatalogService
{
    Task<SystemsBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);
    Task<PortalSystemManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalSystemDto>> ListAsync(
        string? q = null,
        string? category = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
    Task<PortalSystemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PortalSystemDto> CreateAsync(UpsertPortalSystemRequest request, CancellationToken cancellationToken = default);
    Task<PortalSystemDto> UpdateAsync(Guid id, UpsertPortalSystemRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task RecordClickAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UploadSystemIconResponseDto> UploadIconAsync(
        Guid id,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);
}
