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
        var kind = ResolveFieldKind(question.Type);
        var options = ParseOptions(question.ExtraDataJson)
            .Select(pair => new HelpDeskFormOptionDto(pair.Key, pair.Value))
            .ToList();

        // File fields stay in schema as note-only.
        if (question.Type.Contains("QuestionTypeFile", StringComparison.Ordinal))
        {
            kind = "file";
        }

        return new HelpDeskFormQuestionDto(
            question.Id,
            question.Name,
            question.Type,
            kind,
            question.IsMandatory && kind != "file",
            question.Description,
            question.DefaultValue,
            question.HorizontalRank,
            options);
    }

    public static string ResolveFieldKind(string type)
    {
        if (type.Contains("QuestionTypeLongText", StringComparison.Ordinal)) return "longtext";
        if (type.Contains("QuestionTypeShortText", StringComparison.Ordinal)) return "text";
        if (type.Contains("QuestionTypeEmail", StringComparison.Ordinal)) return "email";
        if (type.Contains("QuestionTypeNumber", StringComparison.Ordinal)) return "number";
        if (type.Contains("QuestionTypeDate", StringComparison.Ordinal)) return "date";
        if (type.Contains("QuestionTypeRadio", StringComparison.Ordinal)) return "radio";
        if (type.Contains("QuestionTypeCheckbox", StringComparison.Ordinal)) return "checkbox";
        if (type.Contains("QuestionTypeDropdown", StringComparison.Ordinal)) return "dropdown";
        if (type.Contains("QuestionTypeUrgency", StringComparison.Ordinal)) return "urgency";
        if (type.Contains("QuestionTypeItemDropdown", StringComparison.Ordinal)) return "dropdown";
        if (type.Contains("QuestionTypeItem", StringComparison.Ordinal)) return "text";
        if (type.Contains("QuestionTypeObserver", StringComparison.Ordinal)) return "text";
        if (type.Contains("QuestionTypeRequester", StringComparison.Ordinal)) return "text";
        if (type.Contains("QuestionTypeFile", StringComparison.Ordinal)) return "file";
        if (type.Contains("QuestionTypeUserDevice", StringComparison.Ordinal)) return "text";
        return "text";
    }

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
}
