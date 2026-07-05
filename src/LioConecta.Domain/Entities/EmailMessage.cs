using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class EmailMessage : BaseEntity
{
    public EmailMessageStatus Status { get; set; } = EmailMessageStatus.Pending;

    public string ToAddressesJson { get; set; } = "[]";

    public string? CcAddressesJson { get; set; }

    public string? BccAddressesJson { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string? BodyHtml { get; set; }

    public string? BodyText { get; set; }

    public string? TemplateKey { get; set; }

    public string? MetadataJson { get; set; }

    public string? AttachmentsJson { get; set; }

    public short Priority { get; set; }

    public string? IdempotencyKey { get; set; }

    public Guid? CorrelationId { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public string? LastError { get; set; }

    public string? ProviderMessageId { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }

    public DateTimeOffset? NextRetryAt { get; set; }

    public DateTimeOffset? ProcessingStartedAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public Guid? CreatedById { get; set; }

    public Person? CreatedBy { get; set; }
}
