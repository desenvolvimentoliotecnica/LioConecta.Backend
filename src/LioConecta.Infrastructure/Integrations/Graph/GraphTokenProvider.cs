using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class GraphTokenProvider(IOptions<GraphOptions> options)
{
    private static readonly Uri TokenEndpointTemplate =
        new("https://login.microsoftonline.com/{0}/oauth2/v2.0/token");

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var graphOptions = options.Value;
        if (string.IsNullOrWhiteSpace(graphOptions.TenantId)
            || string.IsNullOrWhiteSpace(graphOptions.ClientId)
            || string.IsNullOrWhiteSpace(graphOptions.ClientSecret))
        {
            return null;
        }

        if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAtUtc.AddMinutes(-5))
        {
            return _accessToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAtUtc.AddMinutes(-5))
            {
                return _accessToken;
            }

            using var client = new HttpClient();
            var tokenUri = new Uri(string.Format(
                TokenEndpointTemplate.ToString(),
                Uri.EscapeDataString(graphOptions.TenantId.Trim())));

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = graphOptions.ClientId.Trim(),
                ["client_secret"] = graphOptions.ClientSecret,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials",
            });

            using var response = await client.PostAsync(tokenUri, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Graph token request failed ({(int)response.StatusCode}): {TryReadOAuthError(body)}");
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            _accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var expiresProp)
                ? expiresProp.GetInt32()
                : 3600;
            _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string TryReadOAuthError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error_description", out var description))
            {
                return description.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return body;
    }
}
