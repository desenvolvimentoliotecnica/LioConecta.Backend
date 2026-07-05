using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class EmailAdminService(AppDbContext db) : IEmailAdminService
{
    public async Task<EmailMessageSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var since24h = DateTimeOffset.UtcNow.AddHours(-24);

        var counts = await db.EmailMessages
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Pending = g.Count(m => m.Status == EmailMessageStatus.Pending),
                Processing = g.Count(m => m.Status == EmailMessageStatus.Processing),
                Sent = g.Count(m => m.Status == EmailMessageStatus.Sent),
                Failed = g.Count(m => m.Status == EmailMessageStatus.Failed),
                Cancelled = g.Count(m => m.Status == EmailMessageStatus.Cancelled),
                SentLast24Hours = g.Count(m => m.Status == EmailMessageStatus.Sent && m.SentAt >= since24h),
                FailedLast24Hours = g.Count(m =>
                    m.Status == EmailMessageStatus.Failed &&
                    m.UpdatedAt >= since24h &&
                    m.AttemptCount >= m.MaxAttempts),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (counts is null)
        {
            return new EmailMessageSummaryDto(0, 0, 0, 0, 0, 0, 0, 0);
        }

        var total24h = counts.SentLast24Hours + counts.FailedLast24Hours;
        var successRate = total24h == 0 ? 0 : (double)counts.SentLast24Hours / total24h * 100;

        return new EmailMessageSummaryDto(
            counts.Pending,
            counts.Processing,
            counts.Sent,
            counts.Failed,
            counts.Cancelled,
            counts.SentLast24Hours,
            counts.FailedLast24Hours,
            Math.Round(successRate, 1));
    }

    public async Task<PagedEmailMessagesDto> ListMessagesAsync(
        string? status,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.EmailMessages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<EmailMessageStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(m => m.Status == parsedStatus);
        }

        if (from.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m =>
                m.Subject.Contains(term) ||
                m.ToAddressesJson.Contains(term) ||
                (m.LastError != null && m.LastError.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var pageIndex = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageIndex - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedEmailMessagesDto(
            items.Select(EmailMessageMapper.ToDto).ToList(),
            total,
            pageIndex,
            size);
    }

    public async Task<EmailMessageDto?> GetMessageAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.EmailMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return entity is null ? null : EmailMessageMapper.ToDto(entity);
    }

    public async Task<EmailMessageDto?> RetryMessageAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.EmailMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.Status is EmailMessageStatus.Sent or EmailMessageStatus.Cancelled)
        {
            throw new InvalidOperationException("Mensagens enviadas ou canceladas nao podem ser reprocessadas.");
        }

        var now = DateTimeOffset.UtcNow;
        entity.Status = EmailMessageStatus.Pending;
        entity.AttemptCount = 0;
        entity.LastError = null;
        entity.NextRetryAt = now;
        entity.ProcessingStartedAt = null;
        entity.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return EmailMessageMapper.ToDto(entity);
    }

    public async Task<EmailMessageDto?> CancelMessageAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.EmailMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.Status is EmailMessageStatus.Sent or EmailMessageStatus.Processing)
        {
            throw new InvalidOperationException("Mensagens enviadas ou em processamento nao podem ser canceladas.");
        }

        entity.Status = EmailMessageStatus.Cancelled;
        entity.NextRetryAt = null;
        entity.ProcessingStartedAt = null;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return EmailMessageMapper.ToDto(entity);
    }
}
