using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IServiceRequestService
{
    Task<IReadOnlyList<ServiceRequestDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceRequestDto> CreateAsync(CreateServiceRequestRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequestDto>> GetManagementListAsync(
        ServiceRequestStatus? status,
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> GetManagementDetailAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> ApproveAsync(
        Guid id,
        ApproveServiceRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> RejectAsync(
        Guid id,
        RejectServiceRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> ReplyAsManagerAsync(
        Guid id,
        string? message,
        IReadOnlyList<ServiceRequestAttachmentInput>? attachments,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> ReplyAsRequesterAsync(
        Guid id,
        string? message,
        IReadOnlyList<ServiceRequestAttachmentInput>? attachments,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> FinalizeAsync(
        Guid id,
        FinalizeServiceRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestDto?> ConfirmClosureAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceRequestAttachmentFileDto?> GetAttachmentAsync(
        Guid id,
        string storageFileName,
        CancellationToken cancellationToken = default);
}
