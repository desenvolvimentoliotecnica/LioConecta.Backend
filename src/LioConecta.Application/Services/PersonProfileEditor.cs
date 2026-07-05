using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

internal static class PersonProfileEditor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] EditablePersonalKeys =
    [
        "bio",
        "aboutMe",
        "languages",
        "links",
        "pronouns",
        "availability",
        "mentor",
        "buddy",
        "projects",
        "education",
        "certifications",
        "careerHistory",
    ];

    public static Dictionary<string, object?> LoadPersonalData(Person person)
    {
        return JsonMapper.DeserializeObjectDictionary(person.PersonalDataJson)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    public static void SavePersonalData(Person person, Dictionary<string, object?> data)
    {
        person.PersonalDataJson = JsonMapper.SerializeObjectDictionary(data);
    }

    public static void MergeEditableFields(Person person, IDictionary<string, object?> target)
    {
        var stored = LoadPersonalData(person);
        foreach (var key in EditablePersonalKeys)
        {
            if (stored.TryGetValue(key, out var value) && value is not null)
            {
                target[key] = value;
            }
        }
    }

    public static string? ReadBio(Dictionary<string, object?> personalData)
    {
        foreach (var key in new[] { "aboutMe", "bio" })
        {
            if (!personalData.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            var text = value switch
            {
                string raw => raw,
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
                _ => value.ToString(),
            };

            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        return null;
    }

    public static IReadOnlyList<PersonLanguageDto> DeserializeLanguages(object? value)
    {
        return DeserializeArray(value, static item =>
        {
            var name = ReadStringProperty(item, "name", "Name");
            var level = ReadStringProperty(item, "level", "Level");
            return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(level)
                ? null
                : new PersonLanguageDto(name.Trim(), level.Trim());
        });
    }

    public static IReadOnlyDictionary<string, string> DeserializeLinks(object? value)
    {
        if (value is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = ToJsonText(value);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static IReadOnlyList<PersonSkillDto> NormalizeSkills(IReadOnlyList<PersonSkillDto> skills)
    {
        return skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill.Name))
            .Select(skill => new PersonSkillDto(
                skill.Name.Trim(),
                Math.Clamp(skill.Level, 1, 5),
                Math.Max(0, skill.Endorsements)))
            .Take(20)
            .ToList();
    }

    public static IReadOnlyList<PersonLanguageDto> NormalizeLanguages(IReadOnlyList<PersonLanguageDto> languages)
    {
        return languages
            .Where(language => !string.IsNullOrWhiteSpace(language.Name) && !string.IsNullOrWhiteSpace(language.Level))
            .Select(language => new PersonLanguageDto(language.Name.Trim(), language.Level.Trim()))
            .Take(10)
            .ToList();
    }

    public static IReadOnlyDictionary<string, string> NormalizeLinks(IReadOnlyDictionary<string, string> links)
    {
        var allowedKeys = new[] { "linkedin", "github", "portfolio" };
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in allowedKeys)
        {
            if (!links.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                normalized[key] = trimmed;
            }
        }

        return normalized;
    }

    public static string? NormalizePronouns(string? pronouns)
    {
        if (string.IsNullOrWhiteSpace(pronouns))
        {
            return null;
        }

        var trimmed = pronouns.Trim();
        return trimmed.Length > 40 ? trimmed[..40] : trimmed;
    }

    public static PersonAvailabilityDto NormalizeAvailability(PersonAvailabilityDto availability)
    {
        static string? TrimOrNull(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }

        var workModel = TrimOrNull(availability.WorkModel, 40);
        if (workModel is not null &&
            workModel is not ("Presencial" or "Híbrido" or "Remoto"))
        {
            workModel = "Híbrido";
        }

        return new PersonAvailabilityDto(
            workModel,
            TrimOrNull(availability.Schedule, 80),
            TrimOrNull(availability.Timezone, 60),
            TrimOrNull(availability.Floor, 40),
            TrimOrNull(availability.Room, 40));
    }

    public static PersonContactRefDto? NormalizeContactRef(PersonContactRefDto? contact)
    {
        if (contact is null || string.IsNullOrWhiteSpace(contact.Name))
        {
            return null;
        }

        return new PersonContactRefDto(
            contact.Name.Trim()[..Math.Min(contact.Name.Trim().Length, 120)],
            string.IsNullOrWhiteSpace(contact.Slug) ? null : contact.Slug.Trim(),
            string.IsNullOrWhiteSpace(contact.Since) ? null : contact.Since.Trim()[..Math.Min(contact.Since.Trim().Length, 40)]);
    }

    public static IReadOnlyList<PersonProjectDto> NormalizeProjects(IReadOnlyList<PersonProjectDto> projects)
    {
        return projects
            .Where(project => !string.IsNullOrWhiteSpace(project.Name))
            .Select(project =>
            {
                var status = string.IsNullOrWhiteSpace(project.Status) ? "Ativo" : project.Status.Trim();
                if (status is not ("Ativo" or "Concluído" or "Pausado"))
                {
                    status = "Ativo";
                }

                return new PersonProjectDto(
                    project.Name.Trim(),
                    (project.Role ?? string.Empty).Trim(),
                    (project.Since ?? string.Empty).Trim(),
                    status);
            })
            .Take(15)
            .ToList();
    }

    public static IReadOnlyList<PersonEducationDto> NormalizeEducation(IReadOnlyList<PersonEducationDto> education)
    {
        return education
            .Where(item => !string.IsNullOrWhiteSpace(item.Degree) || !string.IsNullOrWhiteSpace(item.Institution))
            .Select(item => new PersonEducationDto(
                (item.Period ?? string.Empty).Trim(),
                (item.Degree ?? string.Empty).Trim(),
                (item.Institution ?? string.Empty).Trim(),
                string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(),
                string.IsNullOrWhiteSpace(item.Type) ? null : item.Type.Trim()))
            .Take(15)
            .ToList();
    }

    public static IReadOnlyList<PersonCertificationDto> NormalizeCertifications(
        IReadOnlyList<PersonCertificationDto> certifications)
    {
        return certifications
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new PersonCertificationDto(
                item.Name.Trim(),
                (item.Issuer ?? string.Empty).Trim(),
                (item.Year ?? string.Empty).Trim()))
            .Take(15)
            .ToList();
    }

    public static IReadOnlyList<PersonCareerHistoryItemDto> NormalizeCareerHistory(
        IReadOnlyList<PersonCareerHistoryItemDto> items)
    {
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "promotion",
            "transfer",
            "atual",
        };

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .Select(item =>
            {
                var type = string.IsNullOrWhiteSpace(item.Type) ? "atual" : item.Type.Trim().ToLowerInvariant();
                if (!allowedTypes.Contains(type))
                {
                    type = "atual";
                }

                return new PersonCareerHistoryItemDto(
                    type,
                    item.Title.Trim(),
                    (item.Date ?? string.Empty).Trim(),
                    (item.Dept ?? string.Empty).Trim(),
                    (item.Note ?? string.Empty).Trim());
            })
            .Take(15)
            .ToList();
    }

    public static object? ToStoredObject<T>(T? value)
        => value is null ? null : JsonSerializer.SerializeToElement(value, JsonOptions);

    public static List<object?> ToStoredList<T>(IReadOnlyList<T> items)
        => items.Select(item => (object?)JsonSerializer.SerializeToElement(item, JsonOptions)).ToList();

    private static IReadOnlyList<T> DeserializeArray<T>(object? value, Func<JsonElement, T?> selector)
        where T : class
    {
        if (value is null)
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(ToJsonText(value));
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var items = new List<T>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var item = selector(element);
                if (item is not null)
                {
                    items.Add(item);
                }
            }

            return items;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string ToJsonText(object value)
        => value switch
        {
            string text => text,
            JsonElement element => element.GetRawText(),
            _ => JsonSerializer.Serialize(value, JsonOptions),
        };

    private static string? ReadStringProperty(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }

        return null;
    }
}
