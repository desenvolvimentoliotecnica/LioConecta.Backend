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
public class EmailSendEndpointTests : IClassFixture<LioConectaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly LioConectaWebApplicationFactory _factory;

    public EmailSendEndpointTests(LioConectaWebApplicationFactory factory)
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
    public async Task SendEmail_WithRecipientSlug_EnqueuesMessage()
    {
        var payload = new SendEmailRequest(
            null,
            "carlos-mendes",
            "Teste integracao perfil",
            "<p>Corpo do e-mail de teste.</p>",
            ["julia.santos@liotecnica.com.br"],
            null,
            null,
            "profile");

        var response = await _client.PostAsJsonAsync("/api/v1/email/send", payload);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SendEmailResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.MessageId);
        Assert.Equal("Pending", result.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await db.EmailMessages.FirstAsync(m => m.Id == result.MessageId);
        Assert.Contains("carlos.mendes@liotecnica.com.br", message.ToAddressesJson);
        Assert.Contains("julia.santos@liotecnica.com.br", message.CcAddressesJson ?? string.Empty);
        Assert.NotNull(message.CreatedById);
        Assert.Contains("leonardo.mendes@liotecnica.com.br", message.MetadataJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendEmail_ToSelf_EnqueuesMessage()
    {
        var payload = new SendEmailRequest(
            null,
            "leonardo-sabino-mendes",
            "Auto envio",
            "<p>Teste</p>",
            null,
            null,
            null,
            "profile");

        var response = await _client.PostAsJsonAsync("/api/v1/email/send", payload);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SendEmailResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.MessageId);
    }
}
