using System.Net;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class CompassEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CompassEndpointTests(LioConectaWebApplicationFactory factory)
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<SeedDataService>()
                .EnsureSeededAsync().GetAwaiter().GetResult();
        }

        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task Bootstrap_ReturnsEnabledSettings()
    {
        var response = await _client.GetAsync("/api/v1/compass/bootstrap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bootstrap = await response.Content.ReadFromJsonAsync<CompassBootstrapDto>();
        Assert.NotNull(bootstrap);
        Assert.True(bootstrap.Enabled);
    }

    [Fact]
    public async Task Meta_ReturnsHyperionSnapshotAndDimensions()
    {
        var response = await _client.GetAsync("/api/v1/compass/meta");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meta = await response.Content.ReadFromJsonAsync<CompassMetaDto>();
        Assert.NotNull(meta);
        Assert.Equal("Hyperion", meta.Snapshot.SourceSystem);
        Assert.Contains(meta.Directorias, d => d.Contains("B2B"));
        Assert.NotEmpty(meta.Tipos);
    }

    [Fact]
    public async Task Dashboard_ReturnsFourDirectoriasBridge()
    {
        var response = await _client.GetAsync("/api/v1/compass/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content.ReadFromJsonAsync<CompassDashboardDto>();
        Assert.NotNull(dashboard);
        Assert.True(dashboard.BridgeByDiretoria.Count >= 4);
        Assert.NotEmpty(dashboard.Kpis);
    }

    [Fact]
    public async Task Ytd_PaginatesAndFiltersByDiretoria()
    {
        var allResponse = await _client.GetAsync("/api/v1/compass/ytd?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
        var allPage = await allResponse.Content.ReadFromJsonAsync<CompassYtdPageDto>();
        Assert.NotNull(allPage);
        Assert.True(allPage.TotalCount > 0);
        Assert.Equal(10, allPage.Items.Count);

        var filteredResponse = await _client.GetAsync("/api/v1/compass/ytd?page=1&pageSize=50&diretoria=B2B%20S%26S");
        Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
        var filtered = await filteredResponse.Content.ReadFromJsonAsync<CompassYtdPageDto>();
        Assert.NotNull(filtered);
        Assert.All(filtered.Items, item => Assert.Equal("B2B S&S", item.Diretoria));
    }
}
