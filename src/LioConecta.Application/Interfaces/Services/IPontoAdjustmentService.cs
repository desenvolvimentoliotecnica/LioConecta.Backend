using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPontoAdjustmentService
{
    Task<PontoAdjustmentResultDto> CreateAsync(
        CreatePontoAdjustmentDto request,
        IReadOnlyList<PontoAttachmentInput>? attachments = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PontoAdjustmentItemDto>> GetMineAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<PontoAdjustmentDetailDto?> GetMineDetailAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PontoAdjustmentManagementItemDto>> GetManagementListAsync(
        string? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<PontoAdjustmentManagementDetailDto?> GetManagementDetailAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    Task<PontoAttachmentFileDto?> GetManagementAttachmentAsync(
        Guid recordId,
        string storageFileName,
        CancellationToken cancellationToken = default);
}
