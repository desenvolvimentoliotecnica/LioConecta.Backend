using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IEmailQueueService
{
    Task<EmailMessageDto> EnqueueAsync(EmailEnqueueRequest request, CancellationToken cancellationToken);
}

public interface IEmailDispatchService
{
    Task<EmailDispatchResultDto> ProcessBatchAsync(CancellationToken cancellationToken);
}

public interface IEmailAdminService
{
    Task<EmailMessageSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    Task<PagedEmailMessagesDto> ListMessagesAsync(
        string? status,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<EmailMessageDto?> GetMessageAsync(Guid id, CancellationToken cancellationToken);

    Task<EmailMessageDto?> RetryMessageAsync(Guid id, CancellationToken cancellationToken);

    Task<EmailMessageDto?> CancelMessageAsync(Guid id, CancellationToken cancellationToken);
}

public interface ISmtpEmailSender
{
    Task<SmtpSendResult> SendAsync(EmailRuntimeConfiguration config, SmtpSendRequest request, CancellationToken cancellationToken);

    Task<EmailConnectionTestResponse> TestAsync(EmailRuntimeConfiguration config, string? testRecipient, CancellationToken cancellationToken);
}
