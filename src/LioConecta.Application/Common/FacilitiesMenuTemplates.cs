using LioConecta.Application.DTOs;

namespace LioConecta.Application.Common;

public static class FacilitiesMenuTemplates
{
    public static readonly IReadOnlyList<string> MealTypes =
    [
        "breakfast",
        "lunch",
        "afternoon_coffee",
        "dinner",
        "shift",
    ];

    public static readonly IReadOnlyList<MenuSectionTemplateDto> LunchSections =
    [
        new("entrada", "Entrada (Sopas)"),
        new("main_1", "Prato principal 1"),
        new("main_2", "Prato principal 2"),
        new("guarnicao", "Guarnição"),
        new("salada_1", "Salada 1*"),
        new("salada_2", "Salada 2*"),
        new("salada_3", "Salada 3*"),
        new("farofa", "Farofa Qualimax"),
        new("gourmet", "Espaço Gourmet"),
        new("sobremesa", "Sobremesa"),
        new("fruta", "Fruta*"),
        new("light", "Light"),
    ];

    public static MenuTemplatesDto ToTemplatesDto()
        => new(LunchSections, MealTypes);

    public static IReadOnlyList<MenuSectionDto> CreateEmptyLunchSections()
        => LunchSections
            .Select(section => new MenuSectionDto(section.Key, section.Label, string.Empty))
            .ToList();
}
