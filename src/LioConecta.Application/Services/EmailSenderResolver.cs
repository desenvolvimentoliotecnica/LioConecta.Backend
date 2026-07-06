using System.Text.Json;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public static class EmailSenderResolver
{
    public sealed record SenderIdentity(string Address, string Name);

    public static SenderIdentity? ResolveFromMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("senderEmail", out var emailProperty))
            {
                return null;
            }

            var email = emailProperty.GetString();
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var address = email.Trim();
            var name = root.TryGetProperty("senderName", out var nameProperty)
                ? nameProperty.GetString()
                : null;

            return new SenderIdentity(address, string.IsNullOrWhiteSpace(name) ? address : name.Trim());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static SenderIdentity? ResolveFromPerson(Person? person)
    {
        if (person is null || string.IsNullOrWhiteSpace(person.Email))
        {
            return null;
        }

        var address = person.Email.Trim();
        var name = string.IsNullOrWhiteSpace(person.Name) ? address : person.Name.Trim();
        return new SenderIdentity(address, name);
    }

    public static SenderIdentity? Resolve(string? metadataJson, Person? person)
    {
        return ResolveFromMetadata(metadataJson) ?? ResolveFromPerson(person);
    }
}
