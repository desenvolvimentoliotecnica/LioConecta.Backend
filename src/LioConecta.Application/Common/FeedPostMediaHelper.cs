using System.Text.Json;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Common;

public static class FeedPostMediaHelper
{
    public static string NormalizeMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
        {
            return absolute.AbsolutePath;
        }

        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }

    public static IReadOnlyList<string> ExtractMediaUrls(IReadOnlyDictionary<string, object?> metadata)
    {
        var urls = new List<string>();

        if (metadata.TryGetValue("mediaItems", out var rawItems))
        {
            foreach (var item in EnumerateDictionaryItems(rawItems))
            {
                if (item.TryGetValue("url", out var rawUrl))
                {
                    var normalized = NormalizeMediaUrl(rawUrl?.ToString());
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        urls.Add(normalized);
                    }
                }
            }
        }

        if (metadata.TryGetValue("mediaUrl", out var singleUrl))
        {
            var normalized = NormalizeMediaUrl(singleUrl?.ToString());
            if (!string.IsNullOrWhiteSpace(normalized) && !urls.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                urls.Add(normalized);
            }
        }

        return urls;
    }

    public static bool TryResolveMediaUrl(
        IReadOnlyDictionary<string, object?> metadata,
        string requestedMediaUrl,
        out string normalizedMediaUrl)
    {
        normalizedMediaUrl = string.Empty;
        var requested = NormalizeMediaUrl(requestedMediaUrl);
        if (string.IsNullOrWhiteSpace(requested))
        {
            return false;
        }

        foreach (var candidate in ExtractMediaUrls(metadata))
        {
            if (string.Equals(candidate, requested, StringComparison.OrdinalIgnoreCase))
            {
                normalizedMediaUrl = candidate;
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyDictionary<string, object?>? NormalizeMetadataForCreate(
        IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return metadata;
        }

        var normalized = new Dictionary<string, object?>(metadata, StringComparer.OrdinalIgnoreCase);
        if (!normalized.TryGetValue("mediaItems", out var rawItems))
        {
            return normalized;
        }

        var mediaItems = new List<Dictionary<string, object?>>();
        foreach (var item in EnumerateDictionaryItems(rawItems))
        {
            var next = new Dictionary<string, object?>(item, StringComparer.OrdinalIgnoreCase);
            if (!next.ContainsKey("id") || string.IsNullOrWhiteSpace(next["id"]?.ToString()))
            {
                next["id"] = Guid.NewGuid().ToString();
            }

            if (next.TryGetValue("url", out var rawUrl))
            {
                next["url"] = NormalizeMediaUrl(rawUrl?.ToString());
            }

            mediaItems.Add(next);
        }

        if (mediaItems.Count > 0)
        {
            normalized["mediaItems"] = mediaItems;
            normalized["mediaUrl"] = mediaItems[0].GetValueOrDefault("url");
            normalized["mediaType"] = mediaItems[0].GetValueOrDefault("mediaType");
        }

        return normalized;
    }

    public static IReadOnlyDictionary<string, object?> DeserializeMetadata(string metadataJson)
        => JsonMapper.DeserializeObjectDictionary(metadataJson);

    private static IEnumerable<Dictionary<string, object?>> EnumerateDictionaryItems(object? rawItems)
    {
        if (rawItems is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in item.EnumerateObject())
                {
                    dict[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number => property.Value.GetRawText(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => property.Value.GetRawText(),
                    };
                }

                yield return dict;
            }

            yield break;
        }

        if (rawItems is IEnumerable<object?> enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is Dictionary<string, object?> dict)
                {
                    yield return dict;
                }
                else if (item is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var parsed in EnumerateDictionaryItems(element))
                    {
                        yield return parsed;
                    }
                }
            }
        }
    }
}
