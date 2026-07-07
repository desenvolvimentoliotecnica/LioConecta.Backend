using LioConecta.Application.DTOs;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.UnitTests;

public sealed class PayslipIncomeStatementRulesTests
{
    [Fact]
    public void ResolveWithheldFromPayslip_matches_totvs_irf_code_561()
    {
        var payslip = new Payslip
        {
            DeductionsJson =
                """[{"code":"561","label":"IRF (Normal)","amount":364.24},{"code":"511","label":"INSS Normal","amount":653.34}]""",
        };

        Assert.Equal(364.24m, PayslipIncomeStatementRules.ResolveWithheldFromPayslip(payslip));
    }

    [Fact]
    public void ResolveWithheldFromPayslip_sums_irrf_deductions()
    {
        var payslip = new Payslip
        {
            DeductionsJson =
                """[{"code":"202","label":"IRRF","amount":2480.00},{"code":"201","label":"INSS","amount":900.00}]""",
        };

        var withheld = PayslipIncomeStatementRules.ResolveWithheldFromPayslip(payslip);

        Assert.Equal(2480m, withheld);
    }

    [Fact]
    public void EnrichWithheldFromPayslips_fills_missing_months_from_cache()
    {
        var lines = new List<IncomeStatementLineDto>
        {
            new(4, 6084.60m, 0m),
            new(5, 18325.44m, 0m),
        };

        var payslips = new List<Payslip>
        {
            new()
            {
                Year = 2026,
                Month = 4,
                PaymentType = "FOLHA",
                DeductionsJson = """[{"code":"202","label":"IRRF","amount":120.50}]""",
            },
            new()
            {
                Year = 2026,
                Month = 5,
                PaymentType = "FOLHA",
                DeductionsJson = """[{"code":"202","label":"IRRF","amount":980.00}]""",
            },
        };

        var enriched = PayslipIncomeStatementRules.EnrichWithheldFromPayslips(lines, payslips, 2026);

        Assert.Equal(120.50m, enriched[0].Withheld);
        Assert.Equal(980.00m, enriched[1].Withheld);
    }
}
