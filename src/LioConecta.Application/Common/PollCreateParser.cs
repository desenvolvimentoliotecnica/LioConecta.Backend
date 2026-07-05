using System.Text.Json;

namespace LioConecta.Application.Common;

public sealed record ParsedPollCreate(
    string Question,
    IReadOnlyList<string> Options,
    DateTimeOffset? EndsAt,
    string? HeroImageUrl);

public static class PollCreateParser
{
    private const int MinQuestionLength = 5;
    private const int MaxQuestionLength = 300;
    private const int MinOptions = 2;
    private const int MaxOptions = 6;

    public static ParsedPollCreate Parse(string content, IReadOnlyDictionary<string, object?>? metadata)
    {
        var question = content.Trim();
        if (question.Length < MinQuestionLength || question.Length > MaxQuestionLength)
        {
            throw new ArgumentException(
                $"Poll question must be between {MinQuestionLength} and {MaxQuestionLength} characters.",
                nameof(content));
        }

        var options = ParseOptions(metadata);
        if (options.Count < MinOptions || options.Count > MaxOptions)
        {
            throw new ArgumentException(
                $"Poll must have between {MinOptions} and {MaxOptions} options.",
                nameof(metadata));
        }

        var normalized = options
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToList();

        if (normalized.Count != options.Count)
        {
            throw new ArgumentException("Poll options cannot be empty.", nameof(metadata));
        }

        if (normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
        {
            throw new ArgumentException("Poll options must be unique.", nameof(metadata));
        }

        var endsAt = ParseEndsAt(metadata);
        if (endsAt is not null && endsAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Poll end date must be in the future.", nameof(metadata));
        }

        var heroImageUrl = ParseHeroImageUrl(metadata);

        return new ParsedPollCreate(question, normalized, endsAt, heroImageUrl);
    }

    private static IReadOnlyList<string> ParseOptions(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("options", out var raw) || raw is null)
        {
            throw new ArgumentException("Poll options are required.", nameof(metadata));
        }

        if (raw is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Poll options must be an array.", nameof(metadata));
            }

            return element.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString())
                .ToList();
        }

        if (raw is IEnumerable<object?> enumerable)
        {
            return enumerable.Select(item => item?.ToString() ?? string.Empty).ToList();
        }

        throw new ArgumentException("Poll options must be an array.", nameof(metadata));
    }

    private static DateTimeOffset? ParseEndsAt(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("endsAt", out var raw) || raw is null)
        {
            return null;
        }

        var text = raw switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            DateTimeOffset dto => dto.ToString("O"),
            _ => raw.ToString()
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(text, out var endsAt))
        {
            throw new ArgumentException("Poll end date is invalid.", nameof(metadata));
        }

        return endsAt.ToUniversalTime();
    }

    private static string? ParseHeroImageUrl(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("heroImageUrl", out var raw) || raw is null)
        {
            return null;
        }

        var url = raw switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => raw.ToString()
        };

        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.Trim();
    }
}
