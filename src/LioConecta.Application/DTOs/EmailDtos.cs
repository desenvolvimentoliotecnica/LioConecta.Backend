namespace LioConecta.Application.DTOs;

public sealed record EmailConfigurationDto(
    Guid Id,
    bool IsEnabled,
    string FromAddress,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    bool HasPassword,
    bool UseStartTls,
    int TimeoutSeconds,
    int MaxAttempts,
    int InitialRetryDelaySeconds,
    int MaxRetryDelaySeconds,
    int DispatchBatchSize,
    int DispatchIntervalSeconds,
    DateTimeOffset UpdatedAt);

public sealed record UpsertEmailConfigurationRequest(
    bool IsEnabled,
    string FromAddress,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string? SmtpPassword,
    bool UseStartTls,
    int TimeoutSeconds,
    int MaxAttempts,
    int InitialRetryDelaySeconds,
    int MaxRetryDelaySeconds,
    int DispatchBatchSize,
    int DispatchIntervalSeconds);

public sealed record EmailSmtpTestRequest(
    bool IsEnabled,
    string FromAddress,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string? SmtpPassword,
    bool UseStartTls,
    int TimeoutSeconds,
    string? TestRecipient);

public sealed record EmailConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail);

public sealed record EmailRuntimeConfiguration(
    bool IsEnabled,
    string FromAddress,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string? SmtpPassword,
    bool UseStartTls,
    int TimeoutSeconds,
    int MaxAttempts,
    int InitialRetryDelaySeconds,
    int MaxRetryDelaySeconds,
    int DispatchBatchSize,
    int DispatchIntervalSeconds);

public sealed record EmailAttachmentRecord(
    string FileName,
    string ContentType,
    string StoragePath,
    long SizeBytes);

public sealed record EmailAttachmentUploadDto(
    Guid Id,
    string FileName,
    long SizeBytes,
    string ContentType);

public sealed record SendEmailRequest(
    IReadOnlyList<string>? To,
    string? RecipientSlug,
    string Subject,
    string? BodyHtml,
    IReadOnlyList<string>? Cc,
    IReadOnlyList<string>? Bcc,
    IReadOnlyList<Guid>? AttachmentIds,
    string? Source);

public sealed record SendEmailResponse(
    Guid MessageId,
    string Status);

public sealed record EmailEnqueueRequest(
    IReadOnlyList<string> To,
    string Subject,
    string? BodyHtml = null,
    string? BodyText = null,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null,
    IReadOnlyList<EmailAttachmentRecord>? Attachments = null,
    string? TemplateKey = null,
    string? MetadataJson = null,
    short Priority = 0,
    string? IdempotencyKey = null,
    Guid? CorrelationId = null,
    DateTimeOffset? ScheduledAt = null,
    Guid? CreatedById = null);

public sealed record EmailMessageDto(
    Guid Id,
    string Status,
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc,
    IReadOnlyList<string> Bcc,
    string Subject,
    string? BodyHtml,
    string? BodyText,
    string? TemplateKey,
    string? MetadataJson,
    short Priority,
    string? IdempotencyKey,
    Guid? CorrelationId,
    int AttemptCount,
    int MaxAttempts,
    string? LastError,
    string? ProviderMessageId,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? NextRetryAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record EmailMessageSummaryDto(
    int Pending,
    int Processing,
    int Sent,
    int Failed,
    int Cancelled,
    int SentLast24Hours,
    int FailedLast24Hours,
    double SuccessRateLast24Hours);

public sealed record PagedEmailMessagesDto(
    IReadOnlyList<EmailMessageDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record EmailDispatchResultDto(
    int Processed,
    int Sent,
    int Failed,
    int Skipped);

public sealed record SmtpSendRequest(
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc,
    IReadOnlyList<string> Bcc,
    string Subject,
    string? BodyHtml,
    string? BodyText,
    IReadOnlyList<EmailAttachmentRecord>? Attachments = null);

public sealed record SmtpSendResult(
    bool Success,
    string? MessageId,
    string? ErrorMessage);
