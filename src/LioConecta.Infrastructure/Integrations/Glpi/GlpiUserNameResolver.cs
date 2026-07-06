using System.Collections.Concurrent;
using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed class GlpiUserNameResolver(ILogger<GlpiUserNameResolver> logger)
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    public async Task EnrichRequesterLabelsAsync(
        HttpClient httpClient,
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        IList<GlpiTicketSummary> tickets,
        CancellationToken cancellationToken)
    {
        var ids = tickets
            .Select(t => t.RequesterLabel)
            .Where(IsNumericUserId)
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        const int maxParallel = 8;
        using var gate = new SemaphoreSlim(maxParallel, maxParallel);
        var tasks = ids.Select(async userId =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                await ResolveDisplayNameAsync(httpClient, credentials, sessionToken, userId, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        foreach (var ticket in tickets)
        {
            if (!IsNumericUserId(ticket.RequesterLabel))
            {
                continue;
            }

            if (_cache.TryGetValue(ticket.RequesterLabel!, out var displayName))
            {
                ticket.RequesterLabel = displayName;
            }
        }
    }

    public async Task<string?> ResolveDisplayNameAsync(
        HttpClient httpClient,
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!IsNumericUserId(userId))
        {
            return userId;
        }

        if (_cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var url = $"{credentials.BaseUrl.TrimEnd('/')}/User/{userId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("App-Token", credentials.AppToken);
        request.Headers.Add("Session-Token", sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogDebug("GLPI User/{UserId} lookup failed: {Status}", userId, (int)response.StatusCode);
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var displayName = FormatUserDisplayName(
            ReadString(root, "firstname"),
            ReadString(root, "realname"),
            ReadString(root, "name"));

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            _cache[userId] = displayName;
        }

        return displayName;
    }

    private static bool IsNumericUserId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string FormatUserDisplayName(string? firstName, string? lastName, string? login)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return string.IsNullOrWhiteSpace(login) ? string.Empty : login.Trim();
    }
}
