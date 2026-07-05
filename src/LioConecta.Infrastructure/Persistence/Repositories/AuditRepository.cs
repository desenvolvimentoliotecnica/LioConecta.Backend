using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(AppDbContext db) : IAuditRepository
{
    public async Task AddRangeAsync(IReadOnlyList<AuditEvent> events, CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return;
        }

        db.AuditEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedAuditEventsDto> QueryAsync(
        AuditEventQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filtered = db.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            filtered = filtered.Where(e => e.Action.Contains(query.Action));
        }

        if (query.ActorId.HasValue)
        {
            filtered = filtered.Where(e => e.ActorId == query.ActorId);
        }

        if (!string.IsNullOrWhiteSpace(query.TargetType))
        {
            filtered = filtered.Where(e => e.TargetType == query.TargetType);
        }

        if (query.CorrelationId.HasValue)
        {
            filtered = filtered.Where(e => e.CorrelationId == query.CorrelationId);
        }

        if (query.Source.HasValue)
        {
            filtered = filtered.Where(e => e.Source == query.Source);
        }

        if (query.From.HasValue)
        {
            filtered = filtered.Where(e => e.CreatedAt >= query.From);
        }

        if (query.To.HasValue)
        {
            filtered = filtered.Where(e => e.CreatedAt <= query.To);
        }

        filtered = ApplyHttpStatusFilter(filtered, query.HttpStatus);

        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await filtered
            .Include(e => e.Actor)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AuditEventDto(
                e.Id,
                e.CorrelationId,
                e.TransactionId,
                e.Source,
                e.Action,
                e.ActorId,
                e.Actor != null ? e.Actor.Name : null,
                e.TargetType,
                e.TargetId,
                e.HttpMethod,
                e.Path,
                e.StatusCode,
                e.DurationMs,
                e.DetailsJson,
                e.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedAuditEventsDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<AuditEventSummaryDto> GetSummaryAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var filtered = ApplyDateRange(db.AuditEvents.AsNoTracking(), from, to);

        var totalCount = await filtered.CountAsync(cancellationToken);
        var httpCount = await filtered.CountAsync(e => e.Source == Domain.Enums.AuditSource.HttpRequest, cancellationToken);
        var entityCount = await filtered.CountAsync(e => e.Source == Domain.Enums.AuditSource.EntityChange, cancellationToken);
        var errorCount = await filtered.CountAsync(
            e => e.StatusCode.HasValue && e.StatusCode >= 400,
            cancellationToken);
        var uniqueActors = await filtered
            .Where(e => e.ActorId.HasValue)
            .Select(e => e.ActorId)
            .Distinct()
            .CountAsync(cancellationToken);
        var uniqueActions = await filtered
            .Select(e => e.Action)
            .Distinct()
            .CountAsync(cancellationToken);

        return new AuditEventSummaryDto(
            totalCount,
            httpCount,
            entityCount,
            errorCount,
            uniqueActors,
            uniqueActions);
    }

    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 200);
        return await ApplyDateRange(db.AuditEvents.AsNoTracking(), from, to)
            .Select(e => e.Action)
            .Distinct()
            .OrderBy(action => action)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctTargetTypesAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);
        return await ApplyDateRange(db.AuditEvents.AsNoTracking(), from, to)
            .Select(e => e.TargetType)
            .Distinct()
            .OrderBy(targetType => targetType)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<AuditEvent> ApplyDateRange(
        IQueryable<AuditEvent> query,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.CreatedAt <= to);
        }

        return query;
    }

    private static IQueryable<AuditEvent> ApplyHttpStatusFilter(
        IQueryable<AuditEvent> query,
        string? httpStatus)
    {
        if (string.IsNullOrWhiteSpace(httpStatus))
        {
            return query;
        }

        return httpStatus.Trim().ToLowerInvariant() switch
        {
            "error" => query.Where(e => e.StatusCode.HasValue && e.StatusCode >= 400),
            "success" => query.Where(e => !e.StatusCode.HasValue || e.StatusCode < 400),
            _ => query,
        };
    }
}
