using LioConecta.Application.DTOs;
using LioConecta.Application.Services;

namespace LioConecta.UnitTests;

public class FacilitiesMenuPdfGeneratorTests
{
    [Fact]
    public void Generate_ProducesNonEmptyPdf()
    {
        var week = new WeeklyMenuDto(
            new DateOnly(2026, 7, 6),
            [
                new DailyMenuDto(
                    new DateOnly(2026, 7, 6),
                    "normal",
                    null,
                    [new MealMenuDto("lunch", [new MenuSectionDto("entrada", "Entrada (Sopas)", "Creme de Tomate")])],
                    null,
                    true,
                    null,
                    null),
            ]);

        var bytes = FacilitiesMenuPdfGenerator.Generate(week);

        Assert.NotEmpty(bytes);
        Assert.Equal(0x25, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }
}
