using System.Globalization;
using LioConecta.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LioConecta.Application.Services;

public static class PayslipPdfGenerator
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly Color BorderColor = Color.FromHex("#dbe6f0");
    private static readonly Color MutedColor = Color.FromHex("#5f7489");
    private static readonly Color NetBackground = Color.FromHex("#eef8f0");
    private static readonly Color NetBorder = Color.FromHex("#b9dfc4");
    private static readonly Color NetText = Color.FromHex("#17653a");
    private static readonly Color PaymentBackground = Color.FromHex("#f8fbfd");

    static PayslipPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(PayslipPdfDocumentDto document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(24);
                page.MarginVertical(20);
                page.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Black));

                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(c => RenderHeader(c, document));
                    column.Item().LineHorizontal(1).LineColor(BorderColor);
                    column.Item().Element(c => RenderEmployee(c, document));
                    column.Item().LineHorizontal(1).LineColor(BorderColor);
                    column.Item().Element(c => RenderLineTables(c, document));
                    column.Item().Element(c => RenderBases(c, document));
                    column.Item().Element(c => RenderFooter(c, document));
                });
            });
        }).GeneratePdf();
    }

    private static void RenderHeader(IContainer container, PayslipPdfDocumentDto document)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(document.CompanyName).Bold().FontSize(14);
                column.Item().Text($"CNPJ {document.CompanyCnpj}").FontColor(MutedColor).FontSize(8);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text("RECIBO DE PAGAMENTO DE SALARIO")
                    .FontSize(8).FontColor(MutedColor).LetterSpacing(0.4f);
                column.Item().AlignRight().Text(document.PeriodLabel).Bold().FontSize(13);
                column.Item().AlignRight().Text($"Competencia {document.ReferenceMonth}")
                    .FontColor(MutedColor).FontSize(8);
            });
        });
    }

    private static void RenderEmployee(IContainer container, PayslipPdfDocumentDto document)
    {
        container.PaddingVertical(6).Grid(grid =>
        {
            grid.Columns(3);
            grid.Spacing(8);

            Field(grid, "Funcionario", document.EmployeeName);
            Field(grid, "Matricula", document.EmployeeRegistration);
            Field(grid, "CPF", document.EmployeeCpf);
            Field(grid, "Cargo", document.EmployeeRole);
            Field(grid, "Departamento", document.EmployeeDepartment);
            Field(grid, "Admissao", document.EmployeeAdmissionDate);
        });
    }

    private static void RenderLineTables(IContainer container, PayslipPdfDocumentDto document)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => RenderLinesTable(c, "Proventos", document.Earnings));
            row.ConstantItem(8);
            row.RelativeItem().Element(c => RenderLinesTable(c, "Descontos", document.Deductions));
        });
    }

    private static void RenderLinesTable(IContainer container, string title, IReadOnlyList<PayslipPdfLineDto> lines)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).Text(title).Bold().FontSize(10);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(34);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(62);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Cod.");
                    HeaderCell(header.Cell(), "Descricao");
                    HeaderCell(header.Cell(), "Referencia");
                    HeaderCell(header.Cell(), "Valor", alignRight: true);
                });

                if (lines.Count == 0)
                {
                    table.Cell().ColumnSpan(4).Padding(8).AlignCenter()
                        .Text("Sem lancamentos").FontColor(MutedColor);
                    return;
                }

                foreach (var line in lines)
                {
                    BodyCell(table.Cell(), line.Code);
                    BodyCell(table.Cell(), line.Description);
                    BodyCell(table.Cell(), line.Reference);
                    BodyCell(table.Cell(), FormatCurrency(line.Amount), alignRight: true);
                }
            });
        });
    }

    private static void RenderBases(IContainer container, PayslipPdfDocumentDto document)
    {
        container.PaddingTop(4).Grid(grid =>
        {
            grid.Columns(4);
            grid.Spacing(8);

            Field(grid, "Salario base", FormatCurrency(document.BaseSalary));
            Field(grid, "Base INSS", FormatCurrency(document.BaseInss));
            Field(grid, "Base FGTS", FormatCurrency(document.BaseFgts));
            Field(grid, "FGTS do mes", FormatCurrency(document.FgtsAmount));
        });
    }

    private static void RenderFooter(IContainer container, PayslipPdfDocumentDto document)
    {
        container.PaddingTop(6).Row(row =>
        {
            row.RelativeItem(1.2f).Column(column =>
            {
                TotalRow(column, "Total de proventos", document.TotalEarnings);
                TotalRow(column, "Total de descontos", document.TotalDeductions);

                column.Item().PaddingTop(4).Background(NetBackground).Border(1).BorderColor(NetBorder)
                    .Padding(10).Row(netRow =>
                    {
                        netRow.RelativeItem().Text("Valor liquido a receber").Bold().FontColor(NetText).FontSize(10);
                        netRow.RelativeItem().AlignRight().Text(FormatCurrency(document.NetAmount))
                            .Bold().FontColor(NetText).FontSize(11);
                    });
            });

            row.ConstantItem(12);

            row.RelativeItem().Background(PaymentBackground).Border(1).BorderColor(BorderColor)
                .Padding(12).Grid(grid =>
                {
                    grid.Columns(1);
                    grid.Spacing(8);
                    Field(grid, "Banco", document.BankCode);
                    Field(grid, "Agencia / Conta", $"{document.BankAgency} / {document.BankAccount}");
                    Field(grid, "Data de pagamento", document.PaymentDate);
                });
        });
    }

    private static void Field(GridDescriptor grid, string label, string value)
    {
        grid.Item().Column(column =>
        {
            column.Item().Text(label.ToUpperInvariant()).FontSize(7).FontColor(MutedColor).LetterSpacing(0.3f);
            column.Item().Text(value).Bold().FontSize(9);
        });
    }

    private static void TotalRow(ColumnDescriptor column, string label, decimal amount)
    {
        column.Item().PaddingBottom(6).BorderBottom(1).BorderColor(BorderColor).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(MutedColor);
            row.RelativeItem().AlignRight().Text(FormatCurrency(amount)).Bold();
        });
    }

    private static void HeaderCell(IContainer cell, string text, bool alignRight = false)
    {
        var item = cell.Background(Colors.Black).PaddingVertical(5).PaddingHorizontal(4);
        if (alignRight)
        {
            item.AlignRight().Text(text).FontColor(Colors.White).Bold().FontSize(8);
            return;
        }

        item.Text(text).FontColor(Colors.White).Bold().FontSize(8);
    }

    private static void BodyCell(IContainer cell, string text, bool alignRight = false)
    {
        var item = cell.BorderBottom(1).BorderColor(BorderColor).PaddingVertical(4).PaddingHorizontal(4);
        if (alignRight)
        {
            item.AlignRight().Text(text).FontSize(8);
            return;
        }

        item.Text(text).FontSize(8);
    }

    private static string FormatCurrency(decimal amount) =>
        amount.ToString("C", PtBr);
}
