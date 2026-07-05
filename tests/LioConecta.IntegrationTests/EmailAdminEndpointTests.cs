using System.Net;
using System.Net.Http.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Persistence;
using LioConecta.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.IntegrationTests;

[Collection("WebApp")]
public class EmailAdminEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LioConectaWebApplicationFactory _factory;

    public EmailAdminEndpointTests(LioConectaWebApplicationFactory factory)
    {
        _factory = factory;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<SeedDataService>()
                .EnsureSeededAsync().GetAwaiter().GetResult();
            scope.ServiceProvider.GetRequiredService<IEmailConfigurationService>()
                .EnsureDefaultConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [Fact]
    public async Task GetEmailConfig_ReturnsDefaultConfiguration()
    {
        var response = await _client.GetAsync("/api/v1/admin/email/config");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var config = await response.Content.ReadFromJsonAsync<EmailConfigurationDto>();
        Assert.NotNull(config);
        Assert.NotEqual(Guid.Empty, config.Id);
    }

    [Fact]
    public async Task PutEmailConfig_PersistsSettings()
    {
        var payload = new UpsertEmailConfigurationRequest(
            true,
            "noreply@test.local",
            "LioConecta Test",
            "smtp.test.local",
            587,
            "smtp-user",
            "secret-pass",
            true,
            30,
            5,
            60,
            21600,
            10,
            30);

        var response = await _client.PutAsJsonAsync("/api/v1/admin/email/config", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.EmailConfigurations.SingleAsync();
        Assert.True(entity.IsEnabled);
        Assert.Equal("smtp.test.local", entity.SmtpHost);
        Assert.False(string.IsNullOrWhiteSpace(entity.SmtpPasswordProtected));
    }

    [Fact]
    public async Task GetEmailSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/admin/email/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<EmailMessageSummaryDto>();
        Assert.NotNull(summary);
    }
}
