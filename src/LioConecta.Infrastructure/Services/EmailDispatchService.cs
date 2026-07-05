using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class EmailDispatchService(
    AppDbContext db,
    IEmailConfigurationService configurationService,
    ISmtpEmailSender smtpEmailSender,
    IObservabilityRepository observabilityRepository,
    IHostEnvironment environment,
    ILogger<EmailDispatchService> logger) : IEmailDispatchService
{
    private static readonly TimeSpan StaleProcessingThreshold = TimeSpan.FromMinutes(5);

    public async Task<EmailDispatchResultDto> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var config = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!config.IsEnabled)
        {
            return new EmailDispatchResultDto(0, 0, 0, 0);
        }

        await RecoverStaleProcessingAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var batchSize = config.DispatchBatchSize;
        var candidates = await db.EmailMessages
            .Where(m =>
                (m.Status == EmailMessageStatus.Pending ||
                 m.Status == EmailMessageStatus.Failed) &&
                m.AttemptCount < m.MaxAttempts &&
                m.ScheduledAt <= now &&
                (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderByDescending(m => m.Priority)
            .ThenBy(m => m.ScheduledAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        var sent = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var message in candidates)
        {
            var claimed = await TryClaimAsync(message.Id, now, cancellationToken);
            if (!claimed)
            {
                skipped++;
                continue;
            }

            processed++;
            var tracked = await db.EmailMessages.FirstAsync(m => m.Id == message.Id, cancellationToken);
            var result = await TrySendAsync(tracked, config, cancellationToken);

            if (result.Success)
            {
                sent++;
                tracked.Status = EmailMessageStatus.Sent;
                tracked.SentAt = DateTimeOffset.UtcNow;
                tracked.ProviderMessageId = result.MessageId;
                tracked.LastError = null;
                tracked.ProcessingStartedAt = null;
                tracked.NextRetryAt = null;
                tracked.UpdatedAt = DateTimeOffset.UtcNow;

                await RecordObservabilityAsync(tracked, success: true, null, cancellationToken);
            }
            else
            {
                failed++;
                tracked.AttemptCount += 1;
                tracked.LastError = result.ErrorMessage;
                tracked.ProcessingStartedAt = null;
                tracked.UpdatedAt = DateTimeOffset.UtcNow;

                if (tracked.AttemptCount >= tracked.MaxAttempts)
                {
                    tracked.Status = EmailMessageStatus.Failed;
                    tracked.NextRetryAt = null;
                }
                else
                {
                    tracked.Status = EmailMessageStatus.Failed;
                    tracked.NextRetryAt = now.AddSeconds(EmailRetryCalculator.CalculateDelaySeconds(
                        config.InitialRetryDelaySeconds,
                        config.MaxRetryDelaySeconds,
                        tracked.AttemptCount));
                }

                await RecordObservabilityAsync(tracked, success: false, result.ErrorMessage, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        if (processed > 0)
        {
            logger.LogInformation(
                "Email dispatch batch completed: processed={Processed}, sent={Sent}, failed={Failed}, skipped={Skipped}",
                processed,
                sent,
                failed,
                skipped);
        }

        return new EmailDispatchResultDto(processed, sent, failed, skipped);
    }

    private async Task<bool> TryClaimAsync(Guid messageId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = await db.EmailMessages
            .Where(m =>
                m.Id == messageId &&
                (m.Status == EmailMessageStatus.Pending || m.Status == EmailMessageStatus.Failed))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, EmailMessageStatus.Processing)
                    .SetProperty(m => m.ProcessingStartedAt, now)
                    .SetProperty(m => m.UpdatedAt, now),
                cancellationToken);

        return rows > 0;
    }

    private async Task RecoverStaleProcessingAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(StaleProcessingThreshold);
        await db.EmailMessages
            .Where(m =>
                m.Status == EmailMessageStatus.Processing &&
                m.ProcessingStartedAt != null &&
                m.ProcessingStartedAt < cutoff)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.Status, EmailMessageStatus.Pending)
                    .SetProperty(m => m.ProcessingStartedAt, (DateTimeOffset?)null)
                    .SetProperty(m => m.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }

    private async Task<SmtpSendResult> TrySendAsync(
        EmailMessage message,
        EmailRuntimeConfiguration config,
        CancellationToken cancellationToken)
    {
        var request = new SmtpSendRequest(
            EmailMessageMapper.DeserializeAddresses(message.ToAddressesJson),
            EmailMessageMapper.DeserializeAddresses(message.CcAddressesJson),
            EmailMessageMapper.DeserializeAddresses(message.BccAddressesJson),
            message.Subject,
            message.BodyHtml,
            message.BodyText);

        return await smtpEmailSender.SendAsync(config, request, cancellationToken);
    }

    private async Task RecordObservabilityAsync(
        EmailMessage message,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var metadata = JsonSerializer.Serialize(new
        {
            emailMessageId = message.Id,
            attemptCount = message.AttemptCount,
            toCount = EmailMessageMapper.DeserializeAddresses(message.ToAddressesJson).Count,
        });

        var entity = new ObservabilityEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            EventType = "Integration",
            EventName = success ? "Integration.EmailSent" : "Integration.EmailFailed",
            Severity = (short)(success ? 2 : 4),
            Application = "LioConecta.Api",
            Environment = environment.EnvironmentName,
            CorrelationId = message.CorrelationId ?? Guid.NewGuid(),
            Success = success,
            ErrorType = success ? null : "SmtpSendFailed",
            MetadataJson = metadata,
            CreatedAt = now,
        };

        if (!success && !string.IsNullOrWhiteSpace(errorMessage))
        {
            entity.MetadataJson = JsonSerializer.Serialize(new
            {
                emailMessageId = message.Id,
                attemptCount = message.AttemptCount,
                error = errorMessage.Length > 500 ? errorMessage[..500] : errorMessage,
            });
        }

        await observabilityRepository.AddObservabilityEventsAsync([entity], cancellationToken);
    }
}
