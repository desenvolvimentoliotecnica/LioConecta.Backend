using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed class GlpiOptions
{
    public const string SectionName = "Glpi";

    public string BaseUrl { get; set; } = string.Empty;

    public string AppToken { get; set; } = string.Empty;

    public string UserToken { get; set; } = string.Empty;
}

public sealed class GlpiAdapter(
    HttpClient httpClient,
    IOptions<GlpiOptions> options,
    ILogger<GlpiAdapter> logger) : IGlpiAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string category,
        Guid requesterPersonId,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            logger.LogWarning("Glpi BaseUrl is not configured.");
            return new GlpiTicketResult { Status = "Error" };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "Ticket");
        request.Headers.Add("App-Token", settings.AppToken);
        request.Headers.Add("Authorization", $"user_token {settings.UserToken}");
        request.Content = JsonContent.Create(new
        {
            input = new
            {
                name = title,
                content = description,
                itilcategories_id = category,
                _users_id_requester = requesterPersonId.ToString()
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
        var ticketId = payload?.GetValueOrDefault("id").GetRawText().Trim('"') ?? string.Empty;

        return new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = "New",
            Url = $"{settings.BaseUrl.TrimEnd('/')}/front/ticket.form.php?id={ticketId}"
        };
    }

    public async Task<GlpiTicketResult> GetTicketStatusAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"Ticket/{ticketId}");
        request.Headers.Add("App-Token", settings.AppToken);
        request.Headers.Add("Authorization", $"user_token {settings.UserToken}");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
        var status = payload?.GetValueOrDefault("status").GetRawText().Trim('"') ?? "Unknown";

        return new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = status,
            Url = $"{settings.BaseUrl.TrimEnd('/')}/front/ticket.form.php?id={ticketId}"
        };
    }
}
