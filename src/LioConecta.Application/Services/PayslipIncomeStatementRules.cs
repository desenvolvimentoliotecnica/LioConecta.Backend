using System.Text.Json;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

internal static class PayslipIncomeStatementRules
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static bool IsIncomeTaxWithholding(string code, string label)
    {
        var normalized = NormalizeCode(code);
        if (normalized is "202" or "203" or "204" or "205" or "561" or "562")
        {
            return true;
        }

        if (label.Contains("IRRF", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (label.Contains("IMPOSTO DE RENDA", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (label.Contains("I.R.R.F", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // TOTVS RM frequentemente rotula como "IRF (Normal)" (sem segundo R).
        if (label.Contains("IRF", StringComparison.OrdinalIgnoreCase)
            && !label.Contains("INSS", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static decimal ResolveWithheldFromPayslip(Payslip payslip)
    {
        var deductions = JsonSerializer.Deserialize<List<PayslipLineDto>>(payslip.DeductionsJson, JsonOptions) ?? [];
        return deductions
            .Where(line => IsIncomeTaxWithholding(line.Code, line.Label))
            .Sum(line => line.Amount);
    }

    public static IReadOnlyList<IncomeStatementLineDto> EnrichWithheldFromPayslips(
        IReadOnlyList<IncomeStatementLineDto> lines,
        IReadOnlyList<Payslip> payslips,
        int year)
    {
        var folhaByMonth = payslips
            .Where(p => p.Year == year
                        && string.Equals(p.PaymentType, "FOLHA", StringComparison.OrdinalIgnoreCase))
            .GroupBy(p => p.Month)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(p => p.NroPeriodo).First());

        return lines
            .Select(line =>
            {
                if (line.Withheld > 0m)
                {
                    return line;
                }

                if (!folhaByMonth.TryGetValue(line.Month, out var payslip))
                {
                    return line;
                }

                var withheld = ResolveWithheldFromPayslip(payslip);
                return withheld > 0m ? line with { Withheld = withheld } : line;
            })
            .ToList();
    }

    private static string NormalizeCode(string code) => code.Trim().TrimStart('0') switch
    {
        "" => "0",
        var trimmed => trimmed.PadLeft(3, '0')
    };
}
