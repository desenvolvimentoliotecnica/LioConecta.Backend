using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class ObservabilityEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LioConectaWebApplicationFactory _factory;

    public ObservabilityEndpointTests(LioConectaWebApplicationFactory factory)
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

    [Fact]
    public async Task PostTelemetryEvents_PersistsObservabilityEvent()
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
                            message = "integration test error",
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
            .Where(e => e.CorrelationId == correlationId)
            .ToListAsync();

        Assert.Contains(stored, e => e.EventName == "Application.Error");
    }

    [Fact]
    public async Task PostTelemetryPageViews_PersistsPageView()
    {
        var sessionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var payload = new
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
                    referrerTemplate = "/",
                    durationMs = 1200,
                },
            },
        };

        var response = await _client.PostAsJsonAsync("/api/v1/telemetry/page-views", payload);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.PageViews.AsNoTracking()
            .Where(v => v.CorrelationId == correlationId)
            .ToListAsync();

        Assert.Contains(stored, v => v.PageName == "AuditTrail");
    }

    [Fact]
    public async Task GetAdminAuditEvents_PersistsAccessEvent()
    {
        var correlationId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit-events?page=1&pageSize=5");
        request.Headers.Add("X-Correlation-Id", correlationId.ToString());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.AccessEvents.AsNoTracking()
            .Where(e => e.CorrelationId == correlationId)
            .ToListAsync();

        Assert.Contains(stored, e => e.EventName == "Resource.Viewed");
    }

    [Fact]
    public async Task GetSummary_ReturnsKpis()
    {
        var response = await _client.GetAsync("/api/v1/admin/observability/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(summary.TryGetProperty("errorsLast24h", out _));
        Assert.True(summary.TryGetProperty("pageViews", out _));
    }

    [Fact]
    public async Task Investigate_ReturnsTimelineForCorrelationId()
    {
        var correlationId = Guid.NewGuid();

        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/telemetry/page-views"))
        {
            request.Content = JsonContent.Create(new
            {
                sessionId = Guid.NewGuid(),
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
            await _client.SendAsync(request);
        }

        var response = await _client.GetAsync(
            $"/api/v1/admin/observability/investigate?correlationId={correlationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var timeline = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correlationId.ToString(), timeline.GetProperty("correlationId").GetString());
        Assert.True(timeline.GetProperty("items").GetArrayLength() >= 1);
    }
}
