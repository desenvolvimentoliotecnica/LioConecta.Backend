using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Services;

namespace LioConecta.UnitTests;

public class PayslipCompetenceRulesTests
{
    private static readonly DateTime Admission = new(2020, 3, 15);

    [Theory]
    [InlineData(2020, 3, true)]
    [InlineData(2020, 2, false)]
    [InlineData(2026, 6, true)]
    public void IsEligible_respects_admission(int year, int month, bool expected)
    {
        Assert.Equal(expected, PayslipCompetenceRules.IsEligible(year, month, Admission));
    }

    [Fact]
    public void IsFutureCompetence_blocks_competence_after_reference_month()
    {
        var reference = new DateTime(2026, 6, 1);
        Assert.True(PayslipCompetenceRules.IsFutureCompetence(2026, 7, reference));
        Assert.False(PayslipCompetenceRules.IsFutureCompetence(2026, 6, reference));
        Assert.False(PayslipCompetenceRules.IsEligible(2027, 1, Admission));
    }
}

public class PayslipRmMapperTests
{
    [Fact]
    public void MapPaymentTypeLabel_detects_adiantamento()
    {
        var summary = new RmPayslipSummaryRecord
        {
            NroPeriodo = 2,
            HasAdvanceEvent = true,
            HasPayrollEvents = false,
            GrossAmount = 1000m,
            NetAmount = 1000m,
            DeductionAmount = 0m
        };

        Assert.Equal("ADIANTAMENTO", PayslipRmMapper.MapPaymentTypeLabel(summary));
    }

    [Fact]
    public void MapPaymentTypeLabel_defaults_to_folha()
    {
        var summary = new RmPayslipSummaryRecord
        {
            NroPeriodo = 1,
            HasAdvanceEvent = false,
            HasPayrollEvents = true,
            GrossAmount = 5000m,
            NetAmount = 4200m,
            DeductionAmount = 800m
        };

        Assert.Equal("FOLHA", PayslipRmMapper.MapPaymentTypeLabel(summary));
    }

    [Fact]
    public void ResolveFgtsAmount_uses_rm_value_when_present()
    {
        Assert.Equal(400m, PayslipRmMapper.ResolveFgtsAmount(5000m, 400m));
    }

    [Fact]
    public void ResolveFgtsAmount_falls_back_to_eight_percent_of_base()
    {
        Assert.Equal(400m, PayslipRmMapper.ResolveFgtsAmount(5000m, 0m));
    }
}
