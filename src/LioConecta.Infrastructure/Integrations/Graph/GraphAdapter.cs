using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class GraphOptions
{
    public const string SectionName = "Graph";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}

public sealed class GraphAdapter(
    HttpClient httpClient,
    IOptions<GraphOptions> options,
    ILogger<GraphAdapter> logger) : IGraphAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SyncUserPhotosAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.TenantId))
        {
            logger.LogWarning("Graph TenantId is not configured.");
            return;
        }

        using var response = await httpClient.PostAsync("users/photos/sync", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<GraphDocument>> GetDocumentsAsync(
        string? category,
        CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(category)
            ? "documents"
            : $"documents?category={Uri.EscapeDataString(category)}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var documents = await response.Content.ReadFromJsonAsync<List<GraphDocument>>(JsonOptions, cancellationToken);
        return documents ?? [];
    }

    public async Task<IReadOnlyList<GraphCalendarEvent>> GetCalendarEventsAsync(
        Guid personId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"users/{personId}/calendar?from={from:O}&to={to:O}",
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<List<GraphCalendarEvent>>(JsonOptions, cancellationToken);
        return events ?? [];
    }

    public async Task<IReadOnlyList<GraphPlannerTask>> GetPlannerTasksAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"users/{personId}/planner/tasks", cancellationToken);
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<List<GraphPlannerTask>>(JsonOptions, cancellationToken);
        return tasks ?? [];
    }

    public async Task<string?> GetUserPresenceAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"users/{personId}/presence", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOptions, cancellationToken);
        return payload?.GetValueOrDefault("availability");
    }
}
