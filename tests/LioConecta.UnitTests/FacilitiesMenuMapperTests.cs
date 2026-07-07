using LioConecta.Application.DTOs;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;

namespace LioConecta.UnitTests;

public class FacilitiesMenuMapperTests
{
    [Fact]
    public void ToDailyDto_MapsPublishedPayload()
    {
        var entity = new CafeteriaMenu
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 7),
            PayloadJson = """
                {
                  "dayStatus": "normal",
                  "meals": [
                    {
                      "mealType": "lunch",
                      "sections": [
                        { "key": "entrada", "label": "Entrada (Sopas)", "value": "Creme de tomate" }
                      ]
                    }
                  ]
                }
                """,
            Published = true,
            UpdatedAt = DateTimeOffset.Parse("2026-07-07T12:00:00Z"),
            UpdatedBy = new Person { Email = "facilities@liotecnica.com.br", Name = "Facilities" },
        };

        var dto = FacilitiesMenuMapper.ToDailyDto(entity);

        Assert.Equal(new DateOnly(2026, 7, 7), dto.Date);
        Assert.True(dto.Published);
        Assert.Equal("Creme de tomate", dto.Meals[0].Sections[0].Value);
        Assert.Equal("facilities@liotecnica.com.br", dto.UpdatedBy);
    }

    [Fact]
    public void ToDailyDto_MigratesLegacyItemsJson()
    {
        var entity = new CafeteriaMenu
        {
            Date = new DateOnly(2026, 7, 8),
            ItemsJson = "[\"Sopa\",\"Frango\"]",
            PayloadJson = "{}",
            Published = false,
        };

        var dto = FacilitiesMenuMapper.ToDailyDto(entity);

        Assert.Equal("Sopa", dto.Meals[0].Sections[0].Value);
        Assert.Equal("Frango", dto.Meals[0].Sections[1].Value);
    }

    [Fact]
    public void FromSaveRequest_NormalizesDayStatus()
    {
        var payload = FacilitiesMenuMapper.FromSaveRequest(new SaveDailyMenuRequest(
            "invalid",
            null,
            [new MealMenuDto("lunch", [new MenuSectionDto("entrada", "Entrada (Sopas)", "Salada")])],
            null,
            true));

        var json = FacilitiesMenuMapper.SerializePayload(payload);

        Assert.Contains("\"dayStatus\":\"normal\"", json.Replace(" ", string.Empty), StringComparison.Ordinal);
    }
}
