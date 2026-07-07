using LioConecta.Application.Common;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Seed;

internal sealed record FacilitiesMenuDaySeed(
    Guid Id,
    DateOnly Date,
    string DayStatus,
    string? DayStatusLabel,
    IReadOnlyDictionary<string, string> SectionValues);

internal static class FacilitiesMenuCatalogSeed
{
    public static readonly DateOnly WeekStart = new(2026, 7, 6);

    public static readonly IReadOnlyList<FacilitiesMenuDaySeed> Entries =
    [
        Day(
            SeedIds.FacilitiesMenu20260706,
            new DateOnly(2026, 7, 6),
            "normal",
            null,
            ("entrada", "Creme de Tomate"),
            ("main_1", "Filé de coxa Grelhado"),
            ("main_2", "Nhoque à Bolonhesa"),
            ("guarnicao", "Legumes Panachê"),
            ("salada_1", "Alface"),
            ("salada_2", "Ratatouille"),
            ("salada_3", "Beterraba Ralada"),
            ("farofa", "Picanha"),
            ("sobremesa", "Pudim"),
            ("light", "Abobrinha Recheada com Queijo e legumes")),
        Day(
            SeedIds.FacilitiesMenu20260707,
            new DateOnly(2026, 7, 7),
            "normal",
            null,
            ("entrada", "Lentilha"),
            ("main_1", "Copa Lombo Assado com Laranja e Alecrim"),
            ("main_2", "Rocambole de Carne"),
            ("guarnicao", "Batata Rústica"),
            ("salada_1", "Alface"),
            ("salada_2", "Cenoura Ralada"),
            ("salada_3", "Vinagrete com repolho"),
            ("farofa", "Picanha"),
            ("gourmet", "Baião de Dois"),
            ("sobremesa", "Mousse"),
            ("fruta", "Fruta"),
            ("light", "Fritada de Batata c/ bacon e Brócolis")),
        Day(
            SeedIds.FacilitiesMenu20260708,
            new DateOnly(2026, 7, 8),
            "normal",
            null,
            ("entrada", "Canja"),
            ("main_1", "Picadinho c/ Carne c/ Batata"),
            ("main_2", "Salsicha à Milanesa"),
            ("guarnicao", "Arroz Shop Suey"),
            ("salada_1", "Alface"),
            ("salada_2", "Abóbora na Salsa"),
            ("salada_3", "Salada de Pepino"),
            ("farofa", "Picanha"),
            ("gourmet", "X Bacon"),
            ("sobremesa", "Curau"),
            ("light", "Ovo Pochê com mandioquinha Cozida")),
        Day(
            SeedIds.FacilitiesMenu20260709,
            new DateOnly(2026, 7, 9),
            "holiday",
            "Feriado",
            ("entrada", "Feijão c/ Macarrão"),
            ("main_1", "Filé de Peixe Grelhado"),
            ("main_2", "Linguiça Toscana Festiva"),
            ("guarnicao", "Creme de Milho"),
            ("salada_1", "Alface"),
            ("salada_2", "Grão de Bico c/ Azeitona Preta"),
            ("salada_3", "Grega"),
            ("farofa", "Picanha"),
            ("sobremesa", "Flan"),
            ("fruta", "Fruta"),
            ("light", "Feriado")),
        Day(
            SeedIds.FacilitiesMenu20260710,
            new DateOnly(2026, 7, 10),
            "normal",
            null,
            ("entrada", "Canjiquinha"),
            ("main_1", "Coxa de Frango Assado"),
            ("main_2", "Carne Moída Chique"),
            ("guarnicao", "Espaguete ao Sugo"),
            ("salada_1", "Alface"),
            ("salada_2", "Cebola em Conserva"),
            ("salada_3", "Acelga com Manga"),
            ("farofa", "Picanha"),
            ("sobremesa", "Mousse"),
            ("fruta", "Fruta")),
        Day(
            SeedIds.FacilitiesMenu20260711,
            new DateOnly(2026, 7, 11),
            "normal",
            null,
            ("entrada", "Fubá"),
            ("main_1", "Bife Grelhado"),
            ("main_2", "Escondidinho de calabresa"),
            ("guarnicao", "Batata Chips"),
            ("salada_1", "Alface"),
            ("salada_2", "Escarola"),
            ("salada_3", "Escarola com Milho"),
            ("farofa", "Picanha"),
            ("sobremesa", "Pudim")),
        Day(
            SeedIds.FacilitiesMenu20260712,
            new DateOnly(2026, 7, 12),
            "normal",
            null,
            ("main_1", "Fraldinha ao Alho e óleo"),
            ("main_2", "Filé de Frango Grelhado"),
            ("guarnicao", "Batata Bolinha c/ Alecrim"),
            ("salada_1", "Alface"),
            ("salada_2", "Picles"),
            ("salada_3", "Repolho com Cenoura"),
            ("farofa", "Picanha"),
            ("sobremesa", "Curau")),
    ];

    public static CafeteriaMenu ToEntity(FacilitiesMenuDaySeed day, DateTimeOffset seedTime)
    {
        var sections = FacilitiesMenuTemplates.LunchSections
            .Select(template => new MenuSectionPayload
            {
                Key = template.Key,
                Label = template.Label,
                Value = day.SectionValues.GetValueOrDefault(template.Key) ?? string.Empty,
            })
            .ToList();

        var payload = new CafeteriaMenuPayload
        {
            DayStatus = day.DayStatus,
            DayStatusLabel = day.DayStatusLabel,
            Meals =
            [
                new MealMenuPayload
                {
                    MealType = "lunch",
                    Sections = sections,
                },
            ],
        };

        return new CafeteriaMenu
        {
            Id = day.Id,
            Date = day.Date,
            PayloadJson = FacilitiesMenuMapper.SerializePayload(payload),
            ItemsJson = "[]",
            Published = true,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };
    }

    private static FacilitiesMenuDaySeed Day(
        Guid id,
        DateOnly date,
        string dayStatus,
        string? dayStatusLabel,
        params (string Key, string Value)[] sections)
        => new(id, date, dayStatus, dayStatusLabel, sections.ToDictionary(section => section.Key, section => section.Value));
}
