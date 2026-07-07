using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LioConecta.Application.Services;

public static class PayslipRhDocumentPdfGenerator
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    static PayslipRhDocumentPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerateComprovante(PayslipRhDocumentDto document) =>
        BuildDocument("Comprovante de Rendimentos", document, column =>
        {
            column.Item().Text(
                    "Declaramos, para os devidos fins, que o colaborador abaixo identificado "
                    + "possui vínculo empregatício ativo e percebe remuneração conforme demonstrativo "
                    + "da folha de pagamento.")
                .FontSize(10);

            column.Item().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                AddRow(table, "Competência de referência", document.Competence);
                AddRow(table, "Remuneração bruta", FormatMoney(document.GrossAmount));
                AddRow(table, "Total de descontos", FormatMoney(document.DeductionsTotal));
                AddRow(table, "Remuneração líquida", FormatMoney(document.NetAmount));
            });

            column.Item().PaddingTop(12).Text(
                    "Documento emitido eletronicamente pelo portal LioConecta com base nos dados "
                    + "oficiais da folha de pagamento.")
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);
        });

    public static byte[] GenerateCartaConsignacao(PayslipRhDocumentDto document) =>
        BuildDocument("Carta de Consignação", document, column =>
        {
            column.Item().Text(
                    "A empresa abaixo qualificada atesta a margem consignável estimada do colaborador "
                    + "para operações de crédito consignado, calculada sobre a remuneração líquida da "
                    + "última competência disponível.")
                .FontSize(10);

            column.Item().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                AddRow(table, "Competência base", document.Competence);
                AddRow(table, "Remuneração líquida", FormatMoney(document.NetAmount));
                AddRow(table, "Margem consignável (35%)", FormatMoney(document.ConsignableMargin));
            });

            column.Item().PaddingTop(12).Text(
                    "A margem informada é estimativa para fins de análise de crédito. "
                    + "Instituições financeiras devem validar limites conforme política vigente e contratos ativos.")
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);
        });

    private static byte[] BuildDocument(
        string title,
        PayslipRhDocumentDto document,
        Action<ColumnDescriptor> renderBody)
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
                    column.Item().Text(document.CompanyName).Bold().FontSize(14);
                    column.Item().Text(title).SemiBold().FontSize(12);
                    column.Item().LineHorizontal(1);
                    column.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        AddRow(table, "Colaborador", document.EmployeeName);
                        AddRow(table, "Matrícula", document.EmployeeId);
                        AddRow(table, "Cargo", document.JobTitle);
                        AddRow(table, "Departamento", document.Department);
                        AddRow(table, "Emitido em", document.IssuedAt);
                    });

                    column.Item().PaddingTop(16).Column(renderBody);
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(4).Text(label).SemiBold();
        table.Cell().PaddingVertical(4).Text(value);
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("C2", PtBr);
}

public sealed record PayslipRhDocumentDto(
    string CompanyName,
    string EmployeeName,
    string EmployeeId,
    string JobTitle,
    string Department,
    string Competence,
    decimal GrossAmount,
    decimal DeductionsTotal,
    decimal NetAmount,
    decimal ConsignableMargin,
    string IssuedAt);
