using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Persistence.Repositories;

public sealed class AnalyticsRepository(AppDbContext db) : IAnalyticsRepository
{
    private static readonly Dictionary<ServiceCategory, (string Label, string Color)> ServiceCategoryMeta = new()
    {
        [ServiceCategory.RH] = ("RH & Pessoas", "#2563eb"),
        [ServiceCategory.TI] = ("TI & Suporte", "#7c3aed"),
        [ServiceCategory.Facilities] = ("Facilities", "#0d9488"),
        [ServiceCategory.Juridico] = ("Jurídico", "#d97706"),
        [ServiceCategory.Financeiro] = ("Financeiro", "#db2777"),
    };

    public async Task<IReadOnlyList<AnalyticsEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await db.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task AddEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        db.AnalyticsEvents.Add(analyticsEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountEventsByTypeAsync(
        string eventType,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        db.AnalyticsEvents.CountAsync(
            e => e.EventType == eventType && e.OccurredAt >= from && e.OccurredAt <= to,
            cancellationToken);

    public async Task<AnalyticsSnapshotDto> GetSnapshotAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string periodKey,
        CancellationToken cancellationToken = default)
    {
        var activePeople = await db.People.CountAsync(p => p.IsActive, cancellationToken);

        var activeUsersInPeriod = await db.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to && e.PersonId != null)
            .Select(e => e.PersonId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        var feedPosts = await db.FeedPosts.CountAsync(
            p => p.CreatedAt >= from && p.CreatedAt <= to,
            cancellationToken);

        var feedComments = await db.Comments.CountAsync(
            c => c.CreatedAt >= from && c.CreatedAt <= to,
            cancellationToken);

        var feedReactions = await db.Reactions.CountAsync(
            r => r.CreatedAt >= from && r.CreatedAt <= to,
            cancellationToken);

        var comunicados = await db.Comunicados.CountAsync(
            c => c.ArchivedAt == null,
            cancellationToken);

        var comunicadoReads = await db.ComunicadoReads.CountAsync(
            r => r.ReadAt >= from && r.ReadAt <= to,
            cancellationToken);

        var activeGroups = await db.Groups.CountAsync(
            g => g.Status == GroupStatus.Active,
            cancellationToken);

        var groupMembers = await db.GroupMembers.CountAsync(
            m => db.Groups.Any(g => g.Id == m.GroupId && g.Status == GroupStatus.Active),
            cancellationToken);

        var groupPosts = await db.GroupPosts.CountAsync(
            p => p.CreatedAt >= from && p.CreatedAt <= to,
            cancellationToken);

        var notifications = await db.Notifications.CountAsync(
            n => n.CreatedAt >= from && n.CreatedAt <= to,
            cancellationToken);

        var serviceRequests = await db.ServiceRequests.CountAsync(
            r => r.CreatedAt >= from && r.CreatedAt <= to,
            cancellationToken);

        var documents = await db.Documents.CountAsync(cancellationToken);

        var moodChecks = await db.MoodChecks.CountAsync(
            m => m.CreatedAt >= from && m.CreatedAt <= to,
            cancellationToken);

        var activityTrend = await BuildActivityTrendAsync(from, to, periodKey, cancellationToken);
        var serviceBreakdown = await BuildServiceBreakdownAsync(from, to, cancellationToken);
        var departmentEngagement = await BuildDepartmentEngagementAsync(from, to, cancellationToken);
        var topContent = await BuildTopContentAsync(from, to, cancellationToken);

        return new AnalyticsSnapshotDto(
            periodKey,
            activePeople,
            activeUsersInPeriod,
            feedPosts,
            feedComments,
            feedReactions,
            comunicados,
            comunicadoReads,
            activeGroups,
            groupMembers,
            groupPosts,
            notifications,
            serviceRequests,
            documents,
            moodChecks,
            activityTrend,
            serviceBreakdown,
            departmentEngagement,
            topContent);
    }

    private async Task<IReadOnlyList<AnalyticsTrendPointDto>> BuildActivityTrendAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string periodKey,
        CancellationToken cancellationToken)
    {
        var events = await db.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to)
            .Select(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

        var feedPosts = await db.FeedPosts
            .AsNoTracking()
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .Select(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.CreatedAt >= from && n.CreatedAt <= to)
            .Select(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        var serviceRequests = await db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .Select(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var allTimestamps = events
            .Concat(feedPosts)
            .Concat(notifications)
            .Concat(serviceRequests)
            .ToList();

        return periodKey switch
        {
            "7d" => BuildDailyTrend(allTimestamps, from, 7, "ddd"),
            "90d" => BuildMonthlyTrend(allTimestamps, from, 3, "MMM"),
            "12m" => BuildMonthlyTrend(allTimestamps, from, 12, "MMM"),
            _ => BuildWeeklyTrend(allTimestamps, from, 4),
        };
    }

    private static IReadOnlyList<AnalyticsTrendPointDto> BuildDailyTrend(
        IReadOnlyList<DateTimeOffset> timestamps,
        DateTimeOffset from,
        int days,
        string format)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        var points = new List<AnalyticsTrendPointDto>();

        for (var i = 0; i < days; i++)
        {
            var dayStart = from.Date.AddDays(i);
            var dayEnd = dayStart.AddDays(1);
            var count = timestamps.Count(t => t.UtcDateTime >= dayStart && t.UtcDateTime < dayEnd);
            points.Add(new AnalyticsTrendPointDto(dayStart.ToString(format, culture), count));
        }

        return points;
    }

    private static IReadOnlyList<AnalyticsTrendPointDto> BuildWeeklyTrend(
        IReadOnlyList<DateTimeOffset> timestamps,
        DateTimeOffset from,
        int weeks)
    {
        var points = new List<AnalyticsTrendPointDto>();

        for (var i = 0; i < weeks; i++)
        {
            var weekStart = from.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(7);
            var count = timestamps.Count(t => t >= weekStart && t < weekEnd);
            points.Add(new AnalyticsTrendPointDto($"Sem {i + 1}", count));
        }

        return points;
    }

    private static IReadOnlyList<AnalyticsTrendPointDto> BuildMonthlyTrend(
        IReadOnlyList<DateTimeOffset> timestamps,
        DateTimeOffset from,
        int months,
        string format)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        var points = new List<AnalyticsTrendPointDto>();
        var startMonth = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < months; i++)
        {
            var monthStart = startMonth.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var count = timestamps.Count(t =>
            {
                var utc = t.UtcDateTime;
                return utc >= monthStart && utc < monthEnd;
            });
            points.Add(new AnalyticsTrendPointDto(monthStart.ToString(format, culture), count));
        }

        return points;
    }

    private async Task<IReadOnlyList<AnalyticsServiceSliceDto>> BuildServiceBreakdownAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var grouped = await db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped
            .OrderByDescending(g => g.Count)
            .Select(g =>
            {
                var meta = ServiceCategoryMeta.GetValueOrDefault(g.Category, ("Outros", "#64748b"));
                return new AnalyticsServiceSliceDto(meta.Item1, g.Count, meta.Item2);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<AnalyticsDepartmentDto>> BuildDepartmentEngagementAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var people = await db.People
            .AsNoTracking()
            .Where(p => p.IsActive && p.Dept != null && p.Dept != "")
            .Select(p => new { p.Id, Dept = p.Dept! })
            .ToListAsync(cancellationToken);

        if (people.Count == 0)
        {
            return [];
        }

        var activePersonIds = await db.AnalyticsEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to && e.PersonId != null)
            .Select(e => e.PersonId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var activeSet = activePersonIds.ToHashSet();

        return people
            .GroupBy(p => p.Dept)
            .Select(g =>
            {
                var total = g.Count();
                var active = g.Count(p => activeSet.Contains(p.Id));
                var engagement = total == 0 ? 0 : (int)Math.Round(active * 100.0 / total);
                return new AnalyticsDepartmentDto(g.Key, total, Math.Clamp(engagement, 0, 100));
            })
            .OrderByDescending(d => d.Engagement)
            .ToList();
    }

    private async Task<IReadOnlyList<AnalyticsTopItemDto>> BuildTopContentAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var readCounts = await db.ComunicadoReads
            .AsNoTracking()
            .Where(r => r.ReadAt >= from && r.ReadAt <= to)
            .GroupBy(r => r.ComunicadoId)
            .Select(g => new { ComunicadoId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (readCounts.Count > 0)
        {
            var ids = readCounts.Select(r => r.ComunicadoId).ToList();
            var comunicados = await db.Comunicados
                .AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            return readCounts
                .Where(r => comunicados.ContainsKey(r.ComunicadoId))
                .Select(r =>
                {
                    var comunicado = comunicados[r.ComunicadoId];
                    var readerId = comunicado.Slug ?? comunicado.Id.ToString();
                    return new AnalyticsTopItemDto(
                        comunicado.Title,
                        "Comunicado oficial",
                        r.Count,
                        $"/comunicados/leitura?id={Uri.EscapeDataString(readerId)}",
                        "comunicados");
                })
                .ToList();
        }

        var recent = await db.Comunicados
            .AsNoTracking()
            .Where(c => c.ArchivedAt == null)
            .OrderByDescending(c => c.PublishedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        return recent
            .Select(c =>
            {
                var readerId = c.Slug ?? c.Id.ToString();
                return new AnalyticsTopItemDto(
                    c.Title,
                    "Comunicado oficial",
                    0,
                    $"/comunicados/leitura?id={Uri.EscapeDataString(readerId)}",
                    "comunicados");
            })
            .ToList();
    }
}
