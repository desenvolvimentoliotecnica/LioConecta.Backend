using System.Globalization;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LioConecta.Application.Services;

public static class FacilitiesMenuPdfGenerator
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Color HeaderBackground = Color.FromHex("#1e4f8f");
    private static readonly Color BorderColor = Color.FromHex("#cbd5e1");
    private static readonly Color MutedColor = Color.FromHex("#64748b");
    private static readonly Color HolidayColor = Color.FromHex("#b91c1c");
    private static readonly Color GourmetColor = Color.FromHex("#15803d");

    static FacilitiesMenuPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(WeeklyMenuDto week)
    {
        var weekEnd = week.WeekStart.AddDays(6);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(18);
                page.MarginVertical(16);
                page.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Black));

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Element(c => RenderTitle(c, week.WeekStart, weekEnd));
                    column.Item().Element(c => RenderGrid(c, week));
                    column.Item().AlignRight().Text("Liotécnica")
                        .FontSize(9).FontColor(MutedColor);
                });
            });
        }).GeneratePdf();
    }

    public static string BuildFileName(DateOnly weekStart)
    {
        var end = weekStart.AddDays(6);
        return $"cardapio-semanal-{weekStart:yyyy-MM-dd}-a-{end:yyyy-MM-dd}.pdf";
    }

    private static void RenderTitle(IContainer container, DateOnly weekStart, DateOnly weekEnd)
    {
        container.Column(column =>
        {
            column.Item().Text("Cardápio Semanal")
                .Bold().FontSize(16).FontColor(HeaderBackground);
            column.Item().Text(
                    $"{weekStart.ToString("dd/MM/yy", PtBr)} à {weekEnd.ToString("dd/MM/yyyy", PtBr)}")
                .FontSize(10).FontColor(MutedColor);
        });
    }

    private static void RenderGrid(IContainer container, WeeklyMenuDto week)
    {
        var days = week.Days.OrderBy(day => day.Date).ToList();

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.2f);
                foreach (var _ in days)
                {
                    columns.RelativeColumn(1f);
                }
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), string.Empty);
                foreach (var day in days)
                {
                    HeaderCell(header.Cell(), FormatDayHeader(day), day.DayStatus == "holiday");
                }
            });

            foreach (var section in FacilitiesMenuTemplates.LunchSections)
            {
                table.Cell().Element(c => SectionCell(c, section.Label));

                foreach (var day in days)
                {
                    var value = ResolveCellValue(day, section.Key);
                    var isHoliday = day.DayStatus == "holiday";
                    var isGourmet = section.Key == "gourmet" && !string.IsNullOrWhiteSpace(value) && value != "—";
                    var color = isHoliday ? HolidayColor : isGourmet ? GourmetColor : Colors.Black;
                    table.Cell().Element(c => ValueCell(c, value, color));
                }
            }
        });
    }

    private static string ResolveCellValue(DailyMenuDto day, string sectionKey)
    {
        var lunch = day.Meals.FirstOrDefault(meal => meal.MealType == "lunch");
        var sections = lunch?.Sections ?? [];
        var section = sections.FirstOrDefault(item => item.Key == sectionKey);
        var value = section?.Value?.Trim() ?? string.Empty;

        if (day.DayStatus == "holiday" && sectionKey == "light" && string.IsNullOrWhiteSpace(value))
        {
            return day.DayStatusLabel ?? "Feriado";
        }

        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    private static string FormatDayHeader(DailyMenuDto day)
    {
        var label = day.Date.ToString("dddd", PtBr);
        label = char.ToUpper(label[0], PtBr) + label[1..];
        return $"{label}\n{day.Date:dd/MM}";
    }

    private static void HeaderCell(IContainer cell, string text, bool highlightHoliday = false)
    {
        cell.Background(HeaderBackground)
            .Border(0.5f).BorderColor(BorderColor)
            .PaddingVertical(5).PaddingHorizontal(4)
            .AlignCenter().AlignMiddle()
            .Text(text)
            .FontColor(highlightHoliday ? HolidayColor : Colors.White)
            .Bold().FontSize(7.5f);
    }

    private static void SectionCell(IContainer container, string label)
    {
        container.Background(Color.FromHex("#f1f5f9"))
            .Border(0.5f).BorderColor(BorderColor)
            .PaddingVertical(4).PaddingHorizontal(4)
            .AlignMiddle()
            .Text(label).Bold().FontSize(7.5f);
    }

    private static void ValueCell(IContainer container, string value, Color textColor)
    {
        container.Border(0.5f).BorderColor(BorderColor)
            .PaddingVertical(4).PaddingHorizontal(3)
            .AlignMiddle()
            .Text(value)
            .FontSize(7f)
            .FontColor(textColor);
    }
}
