using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IEmailAttachmentService
{
    Task<EmailAttachmentUploadDto> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long sizeBytes,
        Guid uploadedById,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmailAttachmentRecord>> ConsumeAsync(
        IReadOnlyList<Guid> attachmentIds,
        Guid uploadedById,
        CancellationToken cancellationToken);
}

public interface IEmailSendService
{
    Task<SendEmailResponse> SendAsync(
        SendEmailRequest request,
        Guid senderPersonId,
        CancellationToken cancellationToken);
}
