using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LioConecta.Application.Services;

public static class LeaveRequestPdfGenerator
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    static LeaveRequestPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(LeaveRequestPdfModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(36);
                page.MarginVertical(32);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text("LioConecta").Bold().FontSize(14);
                    column.Item().Text("Comprovante de solicitação de férias").SemiBold().FontSize(12);
                    column.Item().LineHorizontal(1);

                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        AddRow(table, "Colaborador", model.EmployeeName);
                        AddRow(table, "Chapa / matrícula", model.EmployeeId);
                        AddRow(table, "E-mail", model.Email);
                        AddRow(table, "Período", model.Period);
                        AddRow(table, "Dias", model.Days);
                        AddRow(table, "Status", model.Status);
                        AddRow(table, "Sync RM", model.RmSyncStatus);
                        AddRow(table, "Protocolo portal", model.PortalId);
                        AddRow(table, "ID RM", model.RmExternalId);
                        AddRow(table, "Solicitado em", model.CreatedAt);
                    });

                    column.Item().PaddingTop(16).Text(
                            "A aprovação formal da solicitação é realizada no RM Labore. "
                            + "Este comprovante registra apenas a solicitação enviada pelo portal LioConecta.")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(model.Notes))
                    {
                        column.Item().PaddingTop(12).Text("Observações").SemiBold();
                        column.Item().Text(model.Notes).FontSize(10);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(4).Text(label).SemiBold();
        table.Cell().PaddingVertical(4).Text(value);
    }

    public static string FormatDate(DateOnly? value) =>
        value?.ToString("dd/MM/yyyy", PtBr) ?? "—";

    public static string FormatDateTime(DateTimeOffset value) =>
        value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", PtBr);
}

public sealed record LeaveRequestPdfModel(
    string EmployeeName,
    string EmployeeId,
    string Email,
    string Period,
    string Days,
    string Status,
    string RmSyncStatus,
    string PortalId,
    string RmExternalId,
    string CreatedAt,
    string? Notes);
