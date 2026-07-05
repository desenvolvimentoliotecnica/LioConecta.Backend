using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class AuditEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LioConectaWebApplicationFactory _factory;

    public AuditEndpointTests(LioConectaWebApplicationFactory factory)
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
    public async Task PutPreferences_CreatesHttpAuditEvent()
    {
        var correlationId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/me/preferences");
        request.Headers.Add("X-Correlation-Id", correlationId.ToString());
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { bookmarks = Array.Empty<string>() }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await db.AuditEvents.AsNoTracking().ToListAsync();

        Assert.Contains(
            events,
            e => e.CorrelationId == correlationId &&
                 e.Source == AuditSource.HttpRequest &&
                 e.Action.Contains("PUT /api/v1/me/preferences", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAuditEvents_RequiresAdmin()
    {
        var response = await _client.GetAsync("/api/v1/admin/audit-events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedAuditEventsDto>();
        Assert.NotNull(payload);
        Assert.True(payload!.Page >= 1);
    }
}
