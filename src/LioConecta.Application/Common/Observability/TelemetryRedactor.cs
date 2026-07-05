using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LioConecta.Application.Common.Observability;

public static class TelemetryRedactor
{
    private const int MaxMessageLength = 500;
    private const int MaxStackLength = 4096;
    private const int MaxUserAgentLength = 512;

    private static readonly HashSet<string> AllowedPropertyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "routeTemplate",
        "module",
        "pageName",
        "component",
        "actionLabel",
        "statusCode",
        "durationMs",
        "errorType",
        "errorMessage",
        "message",
        "filterName",
        "exportFormat",
        "resourceType",
        "resourceId",
        "frontendVersion",
        "browserLanguage",
        "path",
        "correlationId",
        "action",
        "resource",
        "componentStack",
    };

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "authorization",
        "apikey",
        "api_key",
        "clientsecret",
        "client_secret",
        "refreshtoken",
        "refresh_token",
        "accesstoken",
        "access_token",
        "cpf",
        "rg",
    };

    public static string? SanitizeProperties(Dictionary<string, object?>? properties)
    {
        if (properties is null || properties.Count == 0)
        {
            return null;
        }

        var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in properties)
        {
            if (SensitiveKeys.Contains(key))
            {
                continue;
            }

            if (!AllowedPropertyKeys.Contains(key))
            {
                continue;
            }

            sanitized[key] = SanitizeValue(key, value);
        }

        return sanitized.Count == 0
            ? null
            : JsonSerializer.Serialize(sanitized);
    }

    public static string? TruncateUserAgent(string? userAgent) =>
        string.IsNullOrWhiteSpace(userAgent)
            ? null
            : userAgent.Length <= MaxUserAgentLength
                ? userAgent
                : userAgent[..MaxUserAgentLength];

    public static (string? IpAddress, string? IpHash) ResolveIpFields(
        string? remoteIp,
        string ipMode)
    {
        if (string.IsNullOrWhiteSpace(remoteIp))
        {
            return (null, null);
        }

        var normalizedMode = ipMode.Trim().ToLowerInvariant();
        var hash = HashIp(remoteIp);

        return normalizedMode switch
        {
            "full" => (remoteIp, null),
            "hash" => (null, hash),
            _ => (remoteIp, hash),
        };
    }

    public static string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip.Trim()));
        return Convert.ToHexString(bytes)[..32];
    }

    private static object? SanitizeValue(string key, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            value = element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var number) ? number : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => element.ToString(),
            };
        }

        if (value is string text)
        {
            if (key.Contains("stack", StringComparison.OrdinalIgnoreCase))
            {
                return text.Length <= MaxStackLength ? text : text[..MaxStackLength];
            }

            if (key.Contains("message", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                return text.Length <= MaxMessageLength ? text : text[..MaxMessageLength];
            }

            return text;
        }

        return value;
    }
}
