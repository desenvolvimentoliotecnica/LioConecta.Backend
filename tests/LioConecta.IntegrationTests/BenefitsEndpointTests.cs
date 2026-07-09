using System.Net;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class BenefitsEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BenefitsEndpointTests(LioConectaWebApplicationFactory factory)
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
    public async Task Bootstrap_ReturnsCatalogMetadata()
    {
        var response = await _client.GetAsync("/api/v1/rh/benefits/bootstrap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bootstrap = await response.Content.ReadFromJsonAsync<BenefitsBootstrapDto>();
        Assert.NotNull(bootstrap);
        Assert.NotEmpty(bootstrap!.Categories);
        Assert.NotEmpty(bootstrap.Statuses);
    }

    [Fact]
    public async Task Catalog_List_ReturnsSeededItems()
    {
        var response = await _client.GetAsync("/api/v1/rh/benefits/catalog");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<List<BenefitCatalogItemDto>>();
        Assert.NotNull(items);
        Assert.NotEmpty(items);
    }
}
