using System.Net;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class FacilitiesMenuEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LioConectaWebApplicationFactory _factory;

    public FacilitiesMenuEndpointTests(LioConectaWebApplicationFactory factory)
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
    public async Task Bootstrap_ReturnsTemplatesAndCanEditForAdmin()
    {
        var response = await _client.GetAsync("/api/v1/facilities/menu/bootstrap");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bootstrap = await response.Content.ReadFromJsonAsync<MenuEditorBootstrapDto>();
        Assert.NotNull(bootstrap);
        Assert.True(bootstrap.CanEdit);
        Assert.Equal(12, bootstrap.Templates.LunchSections.Count);
    }

    [Fact]
    public async Task SaveAndPublishMenu_AppearsInCalendarEndpoint()
    {
        const string date = "2026-07-07";
        const string weekStart = "2026-07-06";

        var saveResponse = await _client.PutAsJsonAsync(
            $"/api/v1/facilities/menu/{date}",
            new SaveDailyMenuRequest(
                "normal",
                null,
                [new MealMenuDto("lunch", [new MenuSectionDto("entrada", "Entrada (Sopas)", "Canja")])],
                null,
                true));

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);

        var calendarResponse = await _client.GetAsync($"/api/v1/calendar/menu/{date}");
        Assert.Equal(HttpStatusCode.OK, calendarResponse.StatusCode);

        var published = await calendarResponse.Content.ReadFromJsonAsync<DailyMenuDto>();
        Assert.NotNull(published);
        Assert.True(published.Published);
        Assert.Equal("Canja", published.Meals[0].Sections[0].Value);

        var weekResponse = await _client.GetAsync($"/api/v1/facilities/menu/week?start={weekStart}");
        Assert.Equal(HttpStatusCode.OK, weekResponse.StatusCode);

        var week = await weekResponse.Content.ReadFromJsonAsync<WeeklyMenuDto>();
        Assert.NotNull(week);
        Assert.Equal(7, week.Days.Count);
        Assert.Contains(week.Days, day => day.Date == DateOnly.Parse(date) && day.Meals[0].Sections[0].Value == "Canja");
    }

    [Fact]
    public async Task UnpublishedMenu_ReturnsNotFoundOnCalendar()
    {
        const string date = "2026-07-08";

        var saveResponse = await _client.PutAsJsonAsync(
            $"/api/v1/facilities/menu/{date}",
            new SaveDailyMenuRequest(
                "normal",
                null,
                [new MealMenuDto("lunch", [new MenuSectionDto("entrada", "Entrada (Sopas)", "Legumes")])],
                null,
                false));

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);

        var calendarResponse = await _client.GetAsync($"/api/v1/calendar/menu/{date}");
        Assert.Equal(HttpStatusCode.NotFound, calendarResponse.StatusCode);
    }

    [Fact]
    public async Task SendWeeklyEmail_RequiresRecipients()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/facilities/menu/week/send-email",
            new SendFacilitiesMenuEmailRequest(
                DateOnly.Parse("2026-07-06"),
                null,
                false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadWeeklyPdf_ReturnsPdfFile()
    {
        var response = await _client.GetAsync("/api/v1/facilities/menu/week/pdf?start=2026-07-06");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        Assert.Equal(0x25, bytes[0]);
    }
}
