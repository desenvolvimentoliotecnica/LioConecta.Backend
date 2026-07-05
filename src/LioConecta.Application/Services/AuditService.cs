using System.Text.Json;
using LioConecta.Application.Common.Audit;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class AuditService(
    IAuditContextAccessor contextAccessor,
    IAuditRepository auditRepository) : IAuditService
{
    public void Queue(PendingAuditEvent pendingEvent)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return;
        }

        context.PendingEvents.Add(pendingEvent);
    }

    public void RecordHttp(
        string method,
        string path,
        int statusCode,
        int durationMs,
        Guid? actorId,
        string? requestBody)
    {
        var context = contextAccessor.Current;
        if (context is null)
        {
            return;
        }

        var details = requestBody is null
            ? null
            : JsonSerializer.Serialize(new { requestBody = AuditRedactor.RedactJson(requestBody) });

        Queue(new PendingAuditEvent
        {
            Action = $"Http.{method} {path}",
            TargetType = "HttpRequest",
            TargetId = path,
            Source = AuditSource.HttpRequest,
            ActorId = actorId,
            HttpMethod = method,
            Path = path,
            StatusCode = statusCode,
            DurationMs = durationMs,
            DetailsJson = details,
        });
    }

    public void RecordEntityChange(
        EntityStateKind state,
        string entityType,
        string entityId,
        Guid? actorId,
        string? detailsJson)
    {
        Queue(new PendingAuditEvent
        {
            Action = $"Entity.{state}.{entityType}",
            TargetType = entityType,
            TargetId = entityId,
            Source = AuditSource.EntityChange,
            ActorId = actorId,
            DetailsJson = AuditRedactor.RedactJson(detailsJson),
        });
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var context = contextAccessor.Current;
        if (context is null || context.PendingEvents.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var events = context.PendingEvents
            .Select(pending => new AuditEvent
            {
                Id = Guid.NewGuid(),
                CorrelationId = context.CorrelationId,
                TransactionId = context.TransactionId,
                Source = pending.Source,
                Action = pending.Action,
                ActorId = pending.ActorId,
                TargetType = pending.TargetType,
                TargetId = pending.TargetId,
                HttpMethod = pending.HttpMethod,
                Path = pending.Path,
                StatusCode = pending.StatusCode,
                DurationMs = pending.DurationMs,
                DetailsJson = pending.DetailsJson,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        context.PendingEvents.Clear();
        context.SuppressChangeAudit = true;

        try
        {
            await auditRepository.AddRangeAsync(events, cancellationToken);
        }
        finally
        {
            context.SuppressChangeAudit = false;
        }
    }

    public Task<PagedAuditEventsDto> QueryAsync(
        AuditEventQuery query,
        CancellationToken cancellationToken = default) =>
        auditRepository.QueryAsync(query, cancellationToken);

    public Task<AuditEventSummaryDto> GetSummaryAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default) =>
        auditRepository.GetSummaryAsync(from, to, cancellationToken);

    public Task<IReadOnlyList<string>> GetDistinctActionsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit = 100,
        CancellationToken cancellationToken = default) =>
        auditRepository.GetDistinctActionsAsync(from, to, limit, cancellationToken);

    public Task<IReadOnlyList<string>> GetDistinctTargetTypesAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit = 50,
        CancellationToken cancellationToken = default) =>
        auditRepository.GetDistinctTargetTypesAsync(from, to, limit, cancellationToken);
}
