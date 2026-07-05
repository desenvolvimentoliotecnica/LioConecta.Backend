using LioConecta.Application.Services;

namespace LioConecta.UnitTests;

public class TimesheetPeriodResolverTests
{
    [Fact]
    public void Resolve_UsesSixteenToFifteenPeriodForJulyReference()
    {
        var (dataDe, dataAte, endMonth, endYear) = TimesheetPeriodResolver.Resolve(
            7,
            2026,
            TimesheetPeriodResolver.DefaultStartDay,
            TimesheetPeriodResolver.DefaultEndDay,
            new DateTime(2026, 7, 3));

        Assert.Equal(new DateTime(2026, 6, 16), dataDe);
        Assert.Equal(new DateTime(2026, 7, 15), dataAte);
        Assert.Equal(7, endMonth);
        Assert.Equal(2026, endYear);
    }

    [Fact]
    public void ResolveCurrentPeriodEnd_SwitchesAfterEndDay()
    {
        var beforeEnd = TimesheetPeriodResolver.ResolveCurrentPeriodEnd(
            new DateTime(2026, 7, 15),
            TimesheetPeriodResolver.DefaultEndDay);
        var afterEnd = TimesheetPeriodResolver.ResolveCurrentPeriodEnd(
            new DateTime(2026, 7, 16),
            TimesheetPeriodResolver.DefaultEndDay);

        Assert.Equal(7, beforeEnd.EndMonth);
        Assert.Equal(8, afterEnd.EndMonth);
        Assert.Equal(2026, afterEnd.EndYear);
    }

    [Fact]
    public void BuildRecentPeriodOptions_ReturnsDescendingPeriodLabels()
    {
        var options = TimesheetPeriodResolver.BuildRecentPeriodOptions(
            3,
            TimesheetPeriodResolver.DefaultStartDay,
            TimesheetPeriodResolver.DefaultEndDay,
            new DateTime(2026, 7, 3));

        Assert.Equal(3, options.Count);
        Assert.Equal("16/06/2026 - 15/07/2026", options[0].Label);
        Assert.Equal("16/05/2026 - 15/06/2026", options[1].Label);
        Assert.Equal("16/04/2026 - 15/05/2026", options[2].Label);
    }
}
