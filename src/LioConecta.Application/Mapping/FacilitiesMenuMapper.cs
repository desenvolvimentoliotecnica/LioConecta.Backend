using System.Text.Json;
using System.Text.Json.Serialization;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Mapping;

public sealed class CafeteriaMenuPayload
{
    [JsonPropertyName("dayStatus")]
    public string DayStatus { get; set; } = "normal";

    [JsonPropertyName("dayStatusLabel")]
    public string? DayStatusLabel { get; set; }

    [JsonPropertyName("meals")]
    public List<MealMenuPayload> Meals { get; set; } = [];

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public sealed class MealMenuPayload
{
    [JsonPropertyName("mealType")]
    public string MealType { get; set; } = "lunch";

    [JsonPropertyName("sections")]
    public List<MenuSectionPayload> Sections { get; set; } = [];
}

public sealed class MenuSectionPayload
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public static class FacilitiesMenuMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static DailyMenuDto ToDailyDto(CafeteriaMenu entity)
    {
        var payload = DeserializePayload(entity);
        return new DailyMenuDto(
            entity.Date,
            NormalizeDayStatus(payload.DayStatus),
            payload.DayStatusLabel,
            payload.Meals.Select(ToMealDto).ToList(),
            payload.Notes,
            entity.Published,
            entity.UpdatedAt,
            entity.UpdatedBy?.Email ?? entity.UpdatedBy?.Name);
    }

    public static CafeteriaMenuPayload FromSaveRequest(SaveDailyMenuRequest request)
    {
        var meals = request.Meals?.Count > 0
            ? request.Meals.Select(ToMealPayload).ToList()
            : [CreateDefaultLunchPayload()];

        return new CafeteriaMenuPayload
        {
            DayStatus = NormalizeDayStatus(request.DayStatus),
            DayStatusLabel = request.DayStatusLabel,
            Meals = meals,
            Notes = request.Notes,
        };
    }

    public static string SerializePayload(CafeteriaMenuPayload payload)
        => JsonSerializer.Serialize(payload, JsonOptions);

    public static CafeteriaMenuPayload ClonePayload(CafeteriaMenu entity)
        => DeserializePayload(entity);

    public static CafeteriaMenuPayload FromDailyDto(DailyMenuDto day)
        => new()
        {
            DayStatus = NormalizeDayStatus(day.DayStatus),
            DayStatusLabel = day.DayStatusLabel,
            Notes = day.Notes,
            Meals = day.Meals.Select(ToMealPayload).ToList(),
        };

    private static CafeteriaMenuPayload DeserializePayload(CafeteriaMenu entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.PayloadJson) && entity.PayloadJson.Trim() is not ("{}" or "null"))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<CafeteriaMenuPayload>(entity.PayloadJson, JsonOptions);
                if (parsed is not null && parsed.Meals.Count > 0)
                {
                    parsed.DayStatus = NormalizeDayStatus(parsed.DayStatus);
                    return parsed;
                }
            }
            catch (JsonException)
            {
                // fallback to legacy
            }
        }

        return FromLegacyItems(entity.ItemsJson);
    }

    private static CafeteriaMenuPayload FromLegacyItems(string? itemsJson)
    {
        var items = JsonMapper.DeserializeStringList(itemsJson);
        var sections = FacilitiesMenuTemplates.CreateEmptyLunchSections().ToList();
        for (var index = 0; index < items.Count && index < sections.Count; index++)
        {
            sections[index] = sections[index] with { Value = items[index] };
        }

        return new CafeteriaMenuPayload
        {
            DayStatus = "normal",
            Meals =
            [
                new MealMenuPayload
                {
                    MealType = "lunch",
                    Sections = sections.Select(ToSectionPayload).ToList(),
                },
            ],
        };
    }

    private static MealMenuDto ToMealDto(MealMenuPayload meal)
        => new(meal.MealType, meal.Sections.Select(section => new MenuSectionDto(section.Key, section.Label, section.Value)).ToList());

    private static MealMenuPayload ToMealPayload(MealMenuDto meal)
        => new()
        {
            MealType = meal.MealType,
            Sections = meal.Sections.Select(section => new MenuSectionPayload
            {
                Key = section.Key,
                Label = section.Label,
                Value = section.Value ?? string.Empty,
            }).ToList(),
        };

    private static MenuSectionPayload ToSectionPayload(MenuSectionDto section)
        => new()
        {
            Key = section.Key,
            Label = section.Label,
            Value = section.Value ?? string.Empty,
        };

    private static MealMenuPayload CreateDefaultLunchPayload()
        => new()
        {
            MealType = "lunch",
            Sections = FacilitiesMenuTemplates.CreateEmptyLunchSections().Select(ToSectionPayload).ToList(),
        };

    private static string NormalizeDayStatus(string? value)
        => value switch
        {
            "holiday" => "holiday",
            "closed" => "closed",
            _ => "normal",
        };
}
