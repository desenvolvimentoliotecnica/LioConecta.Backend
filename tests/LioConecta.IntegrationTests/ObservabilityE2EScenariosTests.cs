using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

/// <summary>
/// Cenários E2E 1–6 do plano de observabilidade (MD).
/// </summary>
[Collection("WebApp")]
public class ObservabilityE2EScenariosTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LioConectaWebApplicationFactory _factory;

    public ObservabilityE2EScenariosTests(LioConectaWebApplicationFactory factory)
    {
        _factory = factory;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<SeedDataService>()
                .EnsureSeededAsync().GetAwaiter().GetResult();
        }

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <summary>Cenário 1 — Correlation ID propagado em erro HTTP (header + ProblemDetails).</summary>
    [Fact]
    public async Task Scenario1_CorrelationId_PropagatesOnHttpError()
    {
        var correlationId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/feed/posts/{postId}/comments");
        request.Headers.Add("X-Correlation-Id", correlationId.ToString());
        request.Content = JsonContent.Create(new { text = "e2e correlation test" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(correlationId.ToString(), response.Headers.GetValues("X-Correlation-Id").Single());

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correlationId.ToString(), problem.GetProperty("correlationId").GetString());
    }

    /// <summary>Cenário 2 — Ingestão batch de eventos ops com redaction LGPD.</summary>
    [Fact]
    public async Task Scenario2_TelemetryBatch_PersistsWithRedaction()
    {
        var sessionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/telemetry/events");
        request.Headers.Add("X-Correlation-Id", correlationId.ToString());
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                sessionId,
                correlationId,
                events = new[]
                {
                    new
                    {
                        eventType = "Application",
                        eventName = "Application.Error",
                        occurredAt = DateTimeOffset.UtcNow,
                        severity = 4,
                        properties = new
                        {
                            routeTemplate = "/analytics",
                            message = "visible message",
                            password = "must-not-persist",
                            token = "secret-token",
                        },
                    },
                },
            }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.ObservabilityEvents.AsNoTracking()
            .SingleAsync(e => e.CorrelationId == correlationId);

        Assert.Equal("Application.Error", stored.EventName);
        Assert.NotNull(stored.MetadataJson);
        Assert.Contains("visible message", stored.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("must-not-persist", stored.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", stored.MetadataJson, StringComparison.Ordinal);
    }

    /// <summary>Cenário 3 — Page views batch persistidos.</summary>
    [Fact]
    public async Task Scenario3_PageViewBatch_PersistsNavigationEvent()
    {
        var sessionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync("/api/v1/telemetry/page-views", new
        {
            sessionId,
            correlationId,
            views = new[]
            {
                new
                {
                    occurredAt = DateTimeOffset.UtcNow,
                    pageName = "ObservabilityHub",
                    routeTemplate = "/admin/observabilidade",
                    module = "admin",
                    referrerTemplate = "/admin/trilha-auditoria",
                    durationMs = 3200,
                },
            },
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.PageViews.AsNoTracking()
            .SingleAsync(v => v.CorrelationId == correlationId);

        Assert.Equal("ObservabilityHub", stored.PageName);
        Assert.Equal("/admin/observabilidade", stored.RouteTemplate);
        Assert.Equal(3200, stored.DurationMs);
    }

    /// <summary>Cenário 4 — Access audit em GET admin (allowlist).</summary>
    [Fact]
    public async Task Scenario4_AdminGetAccessAudit_PersistsResourceViewed()
    {
        var correlationId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/admin/observability/summary");
        request.Headers.Add("X-Correlation-Id", correlationId.ToString());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.AccessEvents.AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .ToListAsync();

        Assert.Contains(stored, e => e.EventName == "Resource.Viewed");
        Assert.Contains(stored, e => e.Result == "Success");
    }

    /// <summary>Cenário 5 — APIs admin de consulta retornam KPIs, listas e métricas.</summary>
    [Fact]
    public async Task Scenario5_AdminQueryEndpoints_ReturnAggregatedData()
    {
        var summaryResponse = await _client.GetAsync("/api/v1/admin/observability/summary");
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(summary.TryGetProperty("errorsLast24h", out _));
        Assert.True(summary.TryGetProperty("httpErrorRate", out _));
        Assert.True(summary.TryGetProperty("dailyActiveUsers", out _));

        var errorsResponse = await _client.GetAsync("/api/v1/admin/observability/errors?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, errorsResponse.StatusCode);
        var errors = await errorsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(errors.TryGetProperty("items", out _));
        Assert.True(errors.TryGetProperty("totalCount", out _));

        var metricsResponse = await _client.GetAsync("/api/v1/admin/observability/metrics?period=24h");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        var metrics = await metricsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(metrics.TryGetProperty("requestsPerMinute", out _));
        Assert.True(metrics.TryGetProperty("errorRate", out _));
        Assert.True(metrics.TryGetProperty("p95LatencyMs", out _));
    }

    /// <summary>Cenário 6 — Timeline unificada correlaciona page view, access, ops e audit.</summary>
    [Fact]
    public async Task Scenario6_InvestigateTimeline_UnifiesAllSourcesByCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/v1/telemetry/page-views", new
        {
            sessionId,
            correlationId,
            views = new[]
            {
                new
                {
                    occurredAt = DateTimeOffset.UtcNow,
                    pageName = "AuditTrail",
                    routeTemplate = "/admin/trilha-auditoria",
                    module = "admin",
                },
            },
        });

        using (var telemetryRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/telemetry/events"))
        {
            telemetryRequest.Content = JsonContent.Create(new
            {
                sessionId,
                correlationId,
                events = new[]
                {
                    new
                    {
                        eventType = "Application",
                        eventName = "Application.NetworkError",
                        occurredAt = DateTimeOffset.UtcNow,
                        severity = 3,
                        properties = new { path = "/api/v1/admin/observability/summary", status = 503 },
                    },
                },
            });
            await _client.SendAsync(telemetryRequest);
        }

        using (var accessRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit-events?page=1&pageSize=5"))
        {
            accessRequest.Headers.Add("X-Correlation-Id", correlationId.ToString());
            await _client.SendAsync(accessRequest);
        }

        using (var mutationRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/me/preferences"))
        {
            mutationRequest.Headers.Add("X-Correlation-Id", correlationId.ToString());
            mutationRequest.Content = JsonContent.Create(new { bookmarks = Array.Empty<string>() });
            await _client.SendAsync(mutationRequest);
        }

        var investigateResponse = await _client.GetAsync(
            $"/api/v1/admin/observability/investigate?correlationId={correlationId}");

        Assert.Equal(HttpStatusCode.OK, investigateResponse.StatusCode);

        var timeline = await investigateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sources = timeline.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("source").GetString())
            .Where(source => source is not null)
            .Cast<string>()
            .Distinct()
            .OrderBy(source => source)
            .ToList();

        Assert.Equal(correlationId.ToString(), timeline.GetProperty("correlationId").GetString());
        Assert.Contains("page_view", sources);
        Assert.Contains("access_event", sources);
        Assert.Contains("observability_event", sources);
        Assert.Contains("audit_event", sources);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditCount = await db.AuditEvents.AsNoTracking()
            .CountAsync(e => e.CorrelationId == correlationId && e.Source == AuditSource.HttpRequest);
        Assert.True(auditCount >= 1);
    }

    /// <summary>Smoke OTel — endpoint Prometheus desabilitado em ambiente Testing (documentado).</summary>
    [Fact]
    public async Task OtelSmoke_MetricsEndpoint_DisabledInTestingEnvironment()
    {
        var response = await _client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
