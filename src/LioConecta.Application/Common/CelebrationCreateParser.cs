using System.Text.Json;

namespace LioConecta.Application.Common;

public static class CelebrationCreateParser
{
    private const int MaxMessageLength = 500;
    private const string DefaultMessage = "Parabéns! 🎂";

    public static Guid ParseCelebratedPersonId(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("celebratedPersonId", out var raw) || raw is null)
        {
            throw new ArgumentException("celebratedPersonId is required in metadata.", nameof(metadata));
        }

        var text = raw switch
        {
            Guid guid => guid.ToString(),
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            JsonElement element when element.ValueKind == JsonValueKind.Null => null,
            _ => raw.ToString()
        };

        if (string.IsNullOrWhiteSpace(text) || !Guid.TryParse(text.Trim(), out var personId) || personId == Guid.Empty)
        {
            throw new ArgumentException("celebratedPersonId must be a valid GUID.", nameof(metadata));
        }

        return personId;
    }

    public static string NormalizeMessage(string content)
    {
        var message = (content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            message = DefaultMessage;
        }

        if (message.Length > MaxMessageLength)
        {
            message = message[..MaxMessageLength];
        }

        return message;
    }
}
