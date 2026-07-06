using System.Net;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class PeopleHierarchyEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PeopleHierarchyEndpointTests(LioConectaWebApplicationFactory factory)
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
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Hierarchy_ReturnsManagerPeersAndDirectReports()
    {
        var response = await _client.GetAsync("/api/v1/people/ricardo-souza/hierarchy");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var hierarchy = await response.Content.ReadFromJsonAsync<PersonHierarchyDto>();
        Assert.NotNull(hierarchy);
        Assert.NotNull(hierarchy.Manager);
        Assert.Equal("carlos-mendes", hierarchy.Manager!.Slug);
        Assert.Contains(hierarchy.Chain, m => m.Slug == "carlos-mendes");
        Assert.True(hierarchy.DirectReportsCount >= 0);
    }

    [Fact]
    public async Task Hierarchy_ReturnsNotFoundForUnknownSlug()
    {
        var response = await _client.GetAsync("/api/v1/people/nao-existe-slug/hierarchy");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OrgChart_ReturnsMetadataAndNodes()
    {
        var response = await _client.GetAsync("/api/v1/people/org-chart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orgChart = await response.Content.ReadFromJsonAsync<OrgChartDto>();
        Assert.NotNull(orgChart);
        Assert.True(orgChart.Total >= 0);
        Assert.NotNull(orgChart.Nodes);
        Assert.NotNull(orgChart.RootIds);
        Assert.True(orgChart.OrphanCount >= 0);
        Assert.NotNull(orgChart.UnassignedNodes);
        Assert.True(orgChart.UnassignedCount >= 0);
        Assert.Equal(orgChart.UnassignedNodes.Count, orgChart.UnassignedCount);
        Assert.DoesNotContain(orgChart.Nodes, node => node.ManagerId is null);
        Assert.All(orgChart.UnassignedNodes, node => Assert.Null(node.ManagerId));

        if (orgChart.Total > 0)
        {
            Assert.Equal(orgChart.Total, orgChart.Nodes.Count + orgChart.UnassignedCount);
        }
    }
}
