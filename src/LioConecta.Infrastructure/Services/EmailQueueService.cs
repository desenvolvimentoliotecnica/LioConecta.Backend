using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class EmailQueueService(
    AppDbContext db,
    IEmailConfigurationService configurationService) : IEmailQueueService
{
    public async Task<EmailMessageDto> EnqueueAsync(EmailEnqueueRequest request, CancellationToken cancellationToken)
    {
        if (request.To is null || request.To.Count == 0)
        {
            throw new ArgumentException("Pelo menos um destinatario e obrigatorio.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException("Assunto e obrigatorio.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await db.EmailMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existing is not null)
            {
                return EmailMessageMapper.ToDto(existing);
            }
        }

        var config = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var entity = new EmailMessage
        {
            Id = Guid.NewGuid(),
            Status = EmailMessageStatus.Pending,
            ToAddressesJson = EmailMessageMapper.SerializeAddresses(request.To),
            CcAddressesJson = EmailMessageMapper.SerializeAddresses(request.Cc),
            BccAddressesJson = EmailMessageMapper.SerializeAddresses(request.Bcc),
            Subject = request.Subject.Trim(),
            BodyHtml = request.BodyHtml,
            BodyText = request.BodyText,
            TemplateKey = request.TemplateKey,
            MetadataJson = request.MetadataJson,
            Priority = request.Priority,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? null
                : request.IdempotencyKey.Trim(),
            CorrelationId = request.CorrelationId ?? Guid.NewGuid(),
            AttemptCount = 0,
            MaxAttempts = config.MaxAttempts,
            ScheduledAt = request.ScheduledAt ?? now,
            NextRetryAt = request.ScheduledAt ?? now,
            CreatedById = request.CreatedById,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.EmailMessages.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return EmailMessageMapper.ToDto(entity);
    }
}
