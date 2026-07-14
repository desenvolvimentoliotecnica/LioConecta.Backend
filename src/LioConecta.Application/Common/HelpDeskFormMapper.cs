using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Common;

public static class HelpDeskFormMapper
{
    public static HelpDeskFormSchemaDto ToDto(GlpiFormSchema schema) =>
        new(
            schema.Id,
            schema.Name,
            schema.Description,
            schema.CategoryId,
            schema.Sections
                .Select(section => new HelpDeskFormSectionDto(
                    section.Id,
                    string.IsNullOrWhiteSpace(section.Name) ? " " : section.Name,
                    section.Questions
                        .Where(q => !IsUnsupportedForUi(q.Type))
                        .Select(ToQuestionDto)
                        .ToList()))
                .Where(section => section.Questions.Count > 0 || !string.IsNullOrWhiteSpace(section.Name))
                .ToList());

    public static HelpDeskFormQuestionDto ToQuestionDto(GlpiFormQuestion question)
    {
        var meta = ParseExtraData(question.ExtraDataJson);
        var kind = ResolveFieldKind(question.Type, meta.ItemType);
        var options = ParseOptions(question.ExtraDataJson)
            .Select(pair => new HelpDeskFormOptionDto(pair.Key, pair.Value))
            .ToList();

        var defaultValue = SanitizeDefaultValue(question.DefaultValue, kind);

        return new HelpDeskFormQuestionDto(
            question.Id,
            question.Name,
            question.Type,
            kind,
            question.IsMandatory,
            question.Description,
            defaultValue,
            question.HorizontalRank,
            options,
            meta.ItemType,
            meta.RootItemsId,
            meta.IsMultiple);
    }

    public static string ResolveFieldKind(string type, string? itemType = null)
    {
        if (type.Contains("QuestionTypeLongText", StringComparison.Ordinal)) return "longtext";
        if (type.Contains("QuestionTypeShortText", StringComparison.Ordinal)) return "text";
        if (type.Contains("QuestionTypeEmail", StringComparison.Ordinal)) return "email";
        if (type.Contains("QuestionTypeNumber", StringComparison.Ordinal)) return "number";
        if (type.Contains("QuestionTypeDate", StringComparison.Ordinal)) return "date";
        if (type.Contains("QuestionTypeRadio", StringComparison.Ordinal)) return "radio";
        if (type.Contains("QuestionTypeCheckbox", StringComparison.Ordinal)) return "checkbox";
        if (type.Contains("QuestionTypeDropdown", StringComparison.Ordinal) &&
            !type.Contains("QuestionTypeItemDropdown", StringComparison.Ordinal))
        {
            return "dropdown";
        }

        if (type.Contains("QuestionTypeUrgency", StringComparison.Ordinal)) return "urgency";
        if (type.Contains("QuestionTypeFile", StringComparison.Ordinal)) return "file";
        if (type.Contains("QuestionTypeObserver", StringComparison.Ordinal) ||
            type.Contains("QuestionTypeRequester", StringComparison.Ordinal))
        {
            return "users";
        }

        if (type.Contains("QuestionTypeItemDropdown", StringComparison.Ordinal) ||
            type.Contains("QuestionTypeItem", StringComparison.Ordinal))
        {
            return ResolveItemFieldKind(itemType);
        }

        if (type.Contains("QuestionTypeUserDevice", StringComparison.Ordinal)) return "text";
        return "text";
    }

    public static string? SanitizeDefaultValue(string? raw, string fieldKind)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();
        if (value is "0" or "-1" or "null" or "undefined")
        {
            return null;
        }

        if (value.StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                if (document.RootElement.TryGetProperty("items_id", out var itemsId))
                {
                    var id = itemsId.ValueKind == JsonValueKind.Number
                        ? itemsId.GetInt32()
                        : int.TryParse(itemsId.GetString(), out var parsed) ? parsed : 0;
                    return id > 0 ? id.ToString() : null;
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Avoid leaking JSON placeholders into any input (even if kind fell back to text).
        if (value.Contains("items_id", StringComparison.Ordinal))
        {
            return null;
        }

        return value;
    }

    private static string ResolveItemFieldKind(string? itemType) =>
        itemType?.Trim() switch
        {
            "User" => "user",
            "ITILCategory" => "itilcategory",
            "Location" => "glpiitem",
            _ => string.IsNullOrWhiteSpace(itemType) ? "glpiitem" : "glpiitem",
        };

    private static bool IsUnsupportedForUi(string type) =>
        type.Contains("QuestionTypeUserDevice", StringComparison.Ordinal);

    private static Dictionary<string, string> ParseOptions(string? extraDataJson)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(extraDataJson))
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(extraDataJson);
            if (!document.RootElement.TryGetProperty("options", out var options) ||
                options.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in options.EnumerateObject())
            {
                result[property.Name] = property.Value.GetString()?.Trim() ?? property.Name;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return result;
    }

    private static (string? ItemType, int? RootItemsId, bool IsMultiple) ParseExtraData(string? extraDataJson)
    {
        if (string.IsNullOrWhiteSpace(extraDataJson))
        {
            return (null, null, false);
        }

        try
        {
            using var document = JsonDocument.Parse(extraDataJson);
            var root = document.RootElement;
            string? itemType = null;
            if (root.TryGetProperty("itemtype", out var itemTypeElement))
            {
                itemType = itemTypeElement.GetString()?.Trim();
            }

            int? rootItemsId = null;
            if (root.TryGetProperty("root_items_id", out var rootElement))
            {
                rootItemsId = rootElement.ValueKind switch
                {
                    JsonValueKind.Number => rootElement.GetInt32(),
                    JsonValueKind.String when int.TryParse(rootElement.GetString(), out var parsed) => parsed,
                    _ => null,
                };
                if (rootItemsId is <= 0)
                {
                    rootItemsId = null;
                }
            }

            var isMultiple = false;
            if (root.TryGetProperty("is_multiple_actors", out var multiActors))
            {
                isMultiple = multiActors.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.Number => multiActors.GetInt32() == 1,
                    JsonValueKind.String => multiActors.GetString() is "1" or "true",
                    _ => false,
                };
            }
            else if (root.TryGetProperty("is_multiple_dropdown", out var multiDropdown))
            {
                isMultiple = multiDropdown.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.Number => multiDropdown.GetInt32() == 1,
                    JsonValueKind.String => multiDropdown.GetString() is "1" or "true",
                    _ => false,
                };
            }

            return (itemType, rootItemsId, isMultiple);
        }
        catch (JsonException)
        {
            return (null, null, false);
        }
    }
}
