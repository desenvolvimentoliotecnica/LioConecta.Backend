using System.Globalization;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class PayslipPdfBuilder(
    IPersonRepository personRepository,
    ITotvsRmEmployeeRepository employeeRepository,
    ITotvsRmPayslipRepository payslipRepository)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<PayslipPdfDocumentDto?> BuildAsync(
        Guid personId,
        Payslip payslip,
        CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(personId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var earnings = DeserializeLines(payslip.EarningsJson);
        var deductions = DeserializeLines(payslip.DeductionsJson);
        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);

        RmEmployeeProfileRecord? profile = null;
        if (!string.IsNullOrWhiteSpace(chapa))
        {
            profile = await employeeRepository.GetProfileByChapaAsync(chapa, cancellationToken);
        }

        var period = !string.IsNullOrWhiteSpace(chapa)
            ? await payslipRepository.GetPayslipPeriodAsync(
                chapa,
                payslip.Year,
                payslip.Month,
                payslip.NroPeriodo,
                cancellationToken)
            : null;

        if (period is not null)
        {
            PayslipRmMapper.NormalizePayslipPeriod(period);
        }

        var gross = earnings.Sum(line => line.Amount);
        var totalDeductions = deductions.Sum(line => line.Amount);
        var baseFgts = period?.BaseFgts ?? gross;
        var fgtsAmount = string.Equals(payslip.PaymentType, "ADIANTAMENTO", StringComparison.OrdinalIgnoreCase)
            ? period?.FgtsAmount ?? 0m
            : PayslipRmMapper.ResolveFgtsAmount(baseFgts, period?.FgtsAmount ?? 0m);

        var paymentDate = payslip.PublishedAt.ToString("dd/MM/yyyy", PtBr);

        return new PayslipPdfDocumentDto(
            "LIO Tecnica",
            "—",
            PayslipRmMapper.BuildPeriodLabel(payslip.Year, payslip.Month),
            $"{payslip.Year}-{payslip.Month:D2}",
            profile?.Nome ?? person.Name,
            chapa ?? person.EmployeeId ?? "—",
            PayslipRmMapper.MaskCpf(profile?.Cpf),
            profile?.FuncaoDescricao ?? person.Title ?? "Colaborador",
            profile?.SecaoDescricao ?? person.Dept ?? "—",
            PayslipRmMapper.FormatAdmissionDate(profile?.DataAdmissao),
            MapLines(earnings),
            MapLines(deductions),
            period?.BaseSalary ?? gross,
            period?.BaseInss ?? gross,
            baseFgts,
            fgtsAmount,
            gross,
            totalDeductions,
            payslip.NetAmount,
            profile?.Banco ?? "—",
            profile?.Agencia ?? "—",
            profile?.Conta ?? "—",
            paymentDate);
    }

    private static IReadOnlyList<PayslipPdfLineDto> MapLines(IReadOnlyList<PayslipLineDto> lines) =>
        lines.Select(line => new PayslipPdfLineDto(
            line.Code,
            line.Label,
            string.IsNullOrWhiteSpace(line.Reference) ? "—" : line.Reference!,
            line.Amount))
            .ToList();

    private static IReadOnlyList<PayslipLineDto> DeserializeLines(string json) =>
        JsonSerializer.Deserialize<List<PayslipLineDto>>(json, JsonOptions) ?? [];
}
