using System.Net;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class UniLioEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UniLioEndpointTests(LioConectaWebApplicationFactory factory)
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
        var response = await _client.GetAsync("/api/v1/unilio/bootstrap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bootstrap = await response.Content.ReadFromJsonAsync<UniLioBootstrapDto>();
        Assert.NotNull(bootstrap);
        Assert.True(bootstrap.Enabled);
    }

    [Fact]
    public async Task Meta_ReturnsDimensionsAndPersona()
    {
        var response = await _client.GetAsync("/api/v1/unilio/meta");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meta = await response.Content.ReadFromJsonAsync<UniLioMetaDto>();
        Assert.NotNull(meta);
        Assert.False(string.IsNullOrWhiteSpace(meta.Persona));
        Assert.NotEmpty(meta.Areas);
        Assert.NotEmpty(meta.ContentTypes);
    }

    [Fact]
    public async Task Dashboard_ReturnsKpisAndRecommendations()
    {
        var response = await _client.GetAsync("/api/v1/unilio/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content.ReadFromJsonAsync<UniLioDashboardDto>();
        Assert.NotNull(dashboard);
        Assert.NotEmpty(dashboard.Kpis);
    }

    [Fact]
    public async Task Catalog_PaginatesCourses()
    {
        var response = await _client.GetAsync("/api/v1/unilio/catalog?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<UniLioCatalogPageDto>();
        Assert.NotNull(catalog);
        Assert.True(catalog.TotalCount > 0);
        Assert.NotEmpty(catalog.Items);
    }

    [Fact]
    public async Task Paths_ReturnsLearningPaths()
    {
        var response = await _client.GetAsync("/api/v1/unilio/paths");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paths = await response.Content.ReadFromJsonAsync<UniLioPathsDto>();
        Assert.NotNull(paths);
        Assert.True(paths.Items.Count >= 5);
    }

    [Fact]
    public async Task MyQuestions_ReturnsLearnerInbox()
    {
        var response = await _client.GetAsync("/api/v1/unilio/me/questions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<UniLioQuestionsPageDto>();
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task ModuleQuestions_IncludesPublicFaq()
    {
        var courseId = Guid.Parse("22222222-2222-2222-2222-22222222001a");
        var moduleId = Guid.Parse("22222222-2222-2222-2222-001a00010000");

        var response = await _client.GetAsync(
            $"/api/v1/unilio/courses/{courseId}/modules/{moduleId}/questions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<UniLioQuestionsPageDto>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Visibility == "public");
    }

    [Fact]
    public async Task CreateModuleQuestion_PersistsPrivateQuestion()
    {
        var courseId = Guid.Parse("22222222-2222-2222-2222-22222222001a");
        var moduleId = Guid.Parse("22222222-2222-2222-2222-001a00020000");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/unilio/courses/{courseId}/modules/{moduleId}/questions",
            new CreateUniLioQuestionRequest("Dúvida de teste de integração", "private"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<UniLioQuestionDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal("private", detail.Visibility);
        Assert.Equal("Dúvida de teste de integração", detail.Body);
    }
}
