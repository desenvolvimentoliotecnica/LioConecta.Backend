using System.Text.Json;
using System.Text.Json.Nodes;

namespace LioConecta.Application.Common.Audit;

public static class AuditRedactor
{
    private const int MaxJsonLength = 8192;

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
    };

    public static string? RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return Truncate(json);
            }

            RedactNode(node);
            var redacted = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            return Truncate(redacted);
        }
        catch (JsonException)
        {
            return Truncate(json);
        }
    }

    private static void RedactNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (property.Key is null)
                    {
                        continue;
                    }

                    if (SensitiveKeys.Contains(property.Key))
                    {
                        obj[property.Key] = "***REDACTED***";
                        continue;
                    }

                    if (property.Value is not null)
                    {
                        RedactNode(property.Value);
                    }
                }

                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        RedactNode(item);
                    }
                }

                break;
        }
    }

    private static string Truncate(string value) =>
        value.Length <= MaxJsonLength ? value : value[..MaxJsonLength] + "…";
}
