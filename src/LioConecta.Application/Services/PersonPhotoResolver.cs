using System.Text.Json;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public static class PersonPhotoResolver
{
    public const string PortalAvatarKey = "portalAvatarUrl";
    public const string PortalAvatarBasePath = "/assets/avatars/animals/avatar-";

    private static readonly HashSet<string> AllowedPortalAvatarIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "cat", "dog", "rabbit", "bear", "fox", "owl", "penguin", "frog",
        "turtle", "elephant", "lion", "monkey", "panda", "koala", "hedgehog", "duck",
        "chick", "bee", "butterfly", "fish", "whale", "dolphin", "snail", "crab",
        "octopus", "giraffe", "zebra", "pig", "cow", "sheep", "deer", "raccoon",
    };

    public static string? GetPortalAvatarUrl(Person person)
    {
        var stored = ReadPortalAvatarFromPersonalData(person);
        return IsPortalAvatarUrl(stored) ? NormalizePath(stored!) : null;
    }

    public static string? GetGraphPhotoUrl(Person person)
    {
        var url = person.PhotoUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url) || IsPortalAvatarUrl(url))
        {
            return null;
        }

        return IsGraphPhotoUrl(url) ? url : null;
    }

    public static string? ResolveEffectivePhotoUrl(Person person)
    {
        var portal = GetPortalAvatarUrl(person);
        if (!string.IsNullOrWhiteSpace(portal))
        {
            return portal;
        }

        var graph = GetGraphPhotoUrl(person);
        if (!string.IsNullOrWhiteSpace(graph))
        {
            return graph;
        }

        var raw = person.PhotoUrl?.Trim();
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    public static void SetPortalAvatarUrl(Person person, string photoUrl)
    {
        var normalized = NormalizeAndValidatePortalAvatar(photoUrl);
        var data = PersonProfileEditor.LoadPersonalData(person);
        data[PortalAvatarKey] = normalized;
        PersonProfileEditor.SavePersonalData(person, data);
    }

    public static void ClearPortalAvatar(Person person)
    {
        var data = PersonProfileEditor.LoadPersonalData(person);
        data.Remove(PortalAvatarKey);
        PersonProfileEditor.SavePersonalData(person, data);
    }

    public static string NormalizeAndValidatePortalAvatar(string photoUrl)
    {
        var normalized = NormalizePath(photoUrl);
        if (!IsPortalAvatarUrl(normalized))
        {
            throw new InvalidOperationException("Avatar inválido. Selecione um avatar do catálogo do portal.");
        }

        var animalId = ExtractAnimalId(normalized);
        if (!AllowedPortalAvatarIds.Contains(animalId))
        {
            throw new InvalidOperationException("Avatar não permitido.");
        }

        return normalized;
    }

    public static bool IsPortalAvatarUrl(string? url)
    {
        var normalized = NormalizePath(url);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var path = normalized.Split('?', 2)[0].ToLowerInvariant();
        return path.StartsWith(PortalAvatarBasePath, StringComparison.Ordinal)
            && path.EndsWith(".png", StringComparison.Ordinal);
    }

    public static bool IsGraphPhotoUrl(string? url)
    {
        var normalized = NormalizePath(url);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Contains("/media/people/", StringComparison.OrdinalIgnoreCase);
        }

        var path = normalized.Split('?', 2)[0].ToLowerInvariant();
        return path.StartsWith("/media/people/", StringComparison.Ordinal);
    }

    private static string? ReadPortalAvatarFromPersonalData(Person person)
    {
        var data = PersonProfileEditor.LoadPersonalData(person);
        if (!data.TryGetValue(PortalAvatarKey, out var value) || value is null)
        {
            return null;
        }

        return ReadString(value);
    }

    private static string ReadString(object value)
        => value switch
        {
            string text => text.Trim(),
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString()?.Trim() ?? string.Empty,
            _ => value.ToString()?.Trim() ?? string.Empty,
        };

    private static string ExtractAnimalId(string normalizedPath)
    {
        var fileName = normalizedPath.Split('?', 2)[0].Split('/').Last();
        const string prefix = "avatar-";
        const string suffix = ".png";
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return fileName[prefix.Length..^suffix.Length];
    }

    private static string NormalizePath(string? url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }
}
