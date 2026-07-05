using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LioConecta.Application.Common.Observability;
using LioConecta.Api.Middleware;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class ObservabilityCoreTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ObservabilityCoreTests(LioConectaWebApplicationFactory factory)
    {
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
    public async Task NotFound_ProblemDetails_IncludesCorrelationId()
    {
        var correlationId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/feed/posts/{postId}/comments");
        request.Headers.Add("X-Correlation-Id", correlationId.ToString());
        request.Content = JsonContent.Create(new { text = "test comment" });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(correlationId.ToString(), response.Headers.GetValues("X-Correlation-Id").Single());

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correlationId.ToString(), problem.GetProperty("correlationId").GetString());
    }

    [Theory]
    [InlineData("/api/v1/admin/audit-events", true)]
    [InlineData("/api/v1/admin/audit-events/summary", true)]
    [InlineData("/api/v1/feed", false)]
    public void AccessAuditRouteMatcher_MatchesAdminRoutes(string path, bool expected)
    {
        var pattern = new AccessAuditRoutePattern("GET", "/api/v1/admin/**", ObservabilityEventNames.Resource.Viewed);
        var actual = AccessAuditRouteMatcher.Matches("GET", path, pattern);
        Assert.Equal(expected, actual);
    }
}
