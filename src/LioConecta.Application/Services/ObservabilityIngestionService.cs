using System.Diagnostics;
using LioConecta.Application.Common;
using LioConecta.Application.Common.Observability;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LioConecta.Application.Services;

public sealed class ObservabilityIngestionService(
    IObservabilityRepository repository,
    IAppSettingsProvider settings,
    IHostEnvironment environment,
    ILogger<ObservabilityIngestionService> logger) : IObservabilityIngestionService
{
    private const int MaxBatchSize = 50;

    public async Task<TelemetryIngestResultDto> IngestEventsAsync(
        TelemetryEventsBatchDto batch,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var pageViewsEnabled = settings.GetBool(AppSettingKeys.ObservabilityPageViewsEnabled, true);

        var accepted = 0;
        var rejected = 0;
        var entities = new List<ObservabilityEvent>();

        foreach (var item in batch.Events.Take(MaxBatchSize))
        {
            if (!pageViewsEnabled &&
                string.Equals(item.EventType, "Navigation", StringComparison.OrdinalIgnoreCase))
            {
                rejected++;
                continue;
            }

            if (!TryMapEvent(item, batch, userId, out var entity))
            {
                rejected++;
                continue;
            }

            entities.Add(entity);
            accepted++;
        }

        rejected += Math.Max(0, batch.Events.Count - MaxBatchSize);

        if (entities.Count > 0)
        {
            await repository.AddObservabilityEventsAsync(entities, cancellationToken);
        }

        return new TelemetryIngestResultDto(accepted, rejected);
    }

    public async Task<TelemetryIngestResultDto> IngestPageViewsAsync(
        TelemetryPageViewsBatchDto batch,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (!settings.GetBool(AppSettingKeys.ObservabilityPageViewsEnabled, true))
        {
            return new TelemetryIngestResultDto(0, batch.Views.Count);
        }

        var accepted = 0;
        var rejected = 0;
        var entities = new List<PageView>();
        var now = DateTimeOffset.UtcNow;

        foreach (var view in batch.Views.Take(MaxBatchSize))
        {
            if (string.IsNullOrWhiteSpace(view.PageName) ||
                string.IsNullOrWhiteSpace(view.RouteTemplate) ||
                string.IsNullOrWhiteSpace(view.Module))
            {
                rejected++;
                continue;
            }

            entities.Add(new PageView
            {
                Id = Guid.NewGuid(),
                OccurredAt = view.OccurredAt == default ? now : view.OccurredAt,
                UserId = userId,
                SessionId = batch.SessionId,
                CorrelationId = batch.CorrelationId,
                PageName = view.PageName.Trim(),
                RouteTemplate = view.RouteTemplate.Trim(),
                Module = view.Module.Trim(),
                ReferrerTemplate = string.IsNullOrWhiteSpace(view.ReferrerTemplate)
                    ? null
                    : view.ReferrerTemplate.Trim(),
                DurationMs = view.DurationMs,
                CreatedAt = now,
                UpdatedAt = now,
            });
            accepted++;
        }

        rejected += Math.Max(0, batch.Views.Count - MaxBatchSize);

        if (entities.Count > 0)
        {
            await repository.AddPageViewsAsync(entities, cancellationToken);
        }

        return new TelemetryIngestResultDto(accepted, rejected);
    }

    public async Task RecordAccessAsync(
        AccessAuditEntry entry,
        string? remoteIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (!settings.GetBool(AppSettingKeys.ObservabilityAccessAuditEnabled, true))
        {
            return;
        }

        var ipMode = settings.GetString(AppSettingKeys.ObservabilityPrivacyIpMode, "both");
        var (ipAddress, ipHash) = TelemetryRedactor.ResolveIpFields(remoteIp, ipMode);
        var now = DateTimeOffset.UtcNow;

        var accessEvent = new AccessEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            EventType = entry.EventType,
            EventName = entry.EventName,
            UserId = entry.UserId,
            UsernameSnapshot = entry.UsernameSnapshot,
            SessionId = entry.SessionId,
            CorrelationId = entry.CorrelationId,
            Resource = entry.Resource ?? entry.Path,
            Action = entry.Action,
            Permission = entry.ReasonCode,
            Result = entry.Result,
            ReasonCode = entry.ReasonCode,
            IpAddress = ipAddress,
            IpHash = ipHash,
            UserAgent = TelemetryRedactor.TruncateUserAgent(userAgent),
            MetadataJson = BuildMetadataJson(entry),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddAccessEventsAsync([accessEvent], cancellationToken);

        logger.LogDebug(
            "Persisted access event {EventName} correlation={CorrelationId}",
            entry.EventName,
            entry.CorrelationId);
    }

    private static string? BuildMetadataJson(AccessAuditEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.MetadataJson))
        {
            if (entry.StatusCode is null)
            {
                return entry.MetadataJson;
            }

            // Merge status into provided metadata when possible.
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(entry.MetadataJson);
                var root = doc.RootElement.Clone();
                var dict = new Dictionary<string, object?>();
                foreach (var prop in root.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                        System.Text.Json.JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        System.Text.Json.JsonValueKind.Null => null,
                        _ => prop.Value.GetRawText(),
                    };
                }

                dict["statusCode"] = entry.StatusCode;
                if (entry.HttpMethod is not null)
                {
                    dict["httpMethod"] = entry.HttpMethod;
                }

                return System.Text.Json.JsonSerializer.Serialize(dict);
            }
            catch
            {
                return entry.MetadataJson;
            }
        }

        return entry.StatusCode is null
            ? null
            : $"{{\"statusCode\":{entry.StatusCode},\"httpMethod\":{(entry.HttpMethod is null ? "null" : $"\"{entry.HttpMethod}\"")}}}";
    }

    private bool TryMapEvent(
        TelemetryEventIngestDto item,
        TelemetryEventsBatchDto batch,
        Guid? userId,
        out ObservabilityEvent entity)
    {
        entity = null!;

        if (string.IsNullOrWhiteSpace(item.EventType) || string.IsNullOrWhiteSpace(item.EventName))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var activity = Activity.Current;
        var metadata = TelemetryRedactor.SanitizeProperties(item.Properties);
        var routeTemplate = item.Properties?.GetValueOrDefault("routeTemplate")?.ToString();

        entity = new ObservabilityEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = item.OccurredAt == default ? now : item.OccurredAt,
            EventType = item.EventType.Trim(),
            EventName = item.EventName.Trim(),
            Severity = item.Severity,
            Application = settings.GetString(AppSettingKeys.ObservabilityOtelServiceName, "LioConecta.Api"),
            Environment = environment.EnvironmentName,
            UserId = userId,
            SessionId = batch.SessionId,
            CorrelationId = batch.CorrelationId,
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString(),
            RouteTemplate = routeTemplate,
            Success = item.Severity < 4,
            MetadataJson = metadata,
            CreatedAt = now,
            UpdatedAt = now,
        };

        return true;
    }
}
