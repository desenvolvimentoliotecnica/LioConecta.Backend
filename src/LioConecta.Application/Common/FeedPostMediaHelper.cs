using System.Text.Json;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Common;

public readonly record struct FeedMediaItemInfo(string Url, string MediaType);

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

    public static string InferMediaType(string? mediaType, string url)
    {
        if (!string.IsNullOrWhiteSpace(mediaType))
        {
            var normalized = mediaType.Trim().ToLowerInvariant();
            if (normalized is "image" or "video")
            {
                return normalized;
            }

            if (normalized.StartsWith("video/", StringComparison.Ordinal))
            {
                return "video";
            }

            if (normalized.StartsWith("image/", StringComparison.Ordinal))
            {
                return "image";
            }
        }

        var path = url.ToLowerInvariant();
        if (path.EndsWith(".mp4") || path.EndsWith(".webm") || path.EndsWith(".mov"))
        {
            return "video";
        }

        return "image";
    }

    public static IReadOnlyList<FeedMediaItemInfo> ExtractMediaItems(IReadOnlyDictionary<string, object?> metadata)
    {
        var items = new List<FeedMediaItemInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string? rawUrl, string? mediaType)
        {
            var normalized = NormalizeMediaUrl(rawUrl);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                return;
            }

            items.Add(new FeedMediaItemInfo(normalized, InferMediaType(mediaType, normalized)));
        }

        if (metadata.TryGetValue("mediaItems", out var rawItems))
        {
            foreach (var item in EnumerateDictionaryItems(rawItems))
            {
                item.TryGetValue("url", out var rawUrl);
                item.TryGetValue("mediaType", out var rawType);
                TryAdd(rawUrl?.ToString(), rawType?.ToString());
            }
        }

        if (metadata.TryGetValue("mediaUrl", out var singleUrl))
        {
            metadata.TryGetValue("mediaType", out var singleType);
            TryAdd(singleUrl?.ToString(), singleType?.ToString());
        }

        if (metadata.TryGetValue("heroImageUrl", out var heroUrl))
        {
            TryAdd(heroUrl?.ToString(), "image");
        }

        return items;
    }

    public static IReadOnlyList<string> ExtractMediaUrls(IReadOnlyDictionary<string, object?> metadata)
        => ExtractMediaItems(metadata).Select(x => x.Url).ToList();

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
