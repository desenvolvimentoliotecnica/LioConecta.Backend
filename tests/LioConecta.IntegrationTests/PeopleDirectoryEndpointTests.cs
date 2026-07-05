using System.Net;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class PeopleDirectoryEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PeopleDirectoryEndpointTests(LioConectaWebApplicationFactory factory)
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
    public async Task Directory_ReturnsGroupedDepartmentsWithEmail()
    {
        var response = await _client.GetAsync("/api/v1/people/directory");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var directory = await response.Content.ReadFromJsonAsync<PersonDirectoryDto>();
        Assert.NotNull(directory);
        Assert.True(directory.Total >= 0);
        Assert.NotNull(directory.Departments);

        if (directory.Total > 0)
        {
            var firstDept = directory.Departments.FirstOrDefault(d => d.People.Count > 0);
            Assert.NotNull(firstDept);
            Assert.False(string.IsNullOrWhiteSpace(firstDept!.People[0].Email));
        }
    }

    [Fact]
    public async Task Directory_FiltersByQuery()
    {
        var response = await _client.GetAsync("/api/v1/people/directory?q=leonardo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var directory = await response.Content.ReadFromJsonAsync<PersonDirectoryDto>();
        Assert.NotNull(directory);
        Assert.All(directory!.Departments.SelectMany(d => d.People), p =>
            Assert.Contains("leonardo", p.Name, StringComparison.OrdinalIgnoreCase));
    }
}
