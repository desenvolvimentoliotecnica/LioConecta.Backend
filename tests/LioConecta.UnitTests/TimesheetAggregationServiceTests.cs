using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Services;

namespace LioConecta.UnitTests;

public class TimesheetAggregationServiceTests
{
    private readonly TimesheetAggregationService _service = new();

    [Fact]
    public void AggregateByDay_BuildsClockInAndClockOutFromOrderedPunches()
    {
        var punches = new List<RmPunchRecord>
        {
            new() { DataPonto = new DateTime(2026, 6, 23), BatidaMinutos = 482, Natureza = 0 },
            new() { DataPonto = new DateTime(2026, 6, 23), BatidaMinutos = 720, Natureza = 2 },
            new() { DataPonto = new DateTime(2026, 6, 23), BatidaMinutos = 780, Natureza = 1 },
            new() { DataPonto = new DateTime(2026, 6, 23), BatidaMinutos = 1051, Natureza = 3 }
        };

        var result = _service.AggregateByDay(punches);
        var day = result[new DateTime(2026, 6, 23)];

        Assert.Equal("08:02", TimesheetAggregationService.FormatClock(day.ClockInMinutes!.Value));
        Assert.Equal("12:00", TimesheetAggregationService.FormatClock(day.LunchOutMinutes!.Value));
        Assert.Equal("13:00", TimesheetAggregationService.FormatClock(day.LunchInMinutes!.Value));
        Assert.Equal("17:31", TimesheetAggregationService.FormatClock(day.ClockOutMinutes!.Value));
        Assert.Equal(60, day.BreakMinutes);
        Assert.Equal("Regular", day.Status);
    }

    [Fact]
    public void AggregateByDay_WithTwoPunches_PlacesEntryAndExitInFirstAndLastColumns()
    {
        var punches = new List<RmPunchRecord>
        {
            new() { DataPonto = new DateTime(2026, 7, 1), BatidaMinutos = 459, Natureza = 0 },
            new() { DataPonto = new DateTime(2026, 7, 1), BatidaMinutos = 1244, Natureza = 2 }
        };

        var result = _service.AggregateByDay(punches);
        var day = result[new DateTime(2026, 7, 1)];

        Assert.Equal("07:39", TimesheetAggregationService.FormatClock(day.ClockInMinutes!.Value));
        Assert.Null(day.LunchOutMinutes);
        Assert.Null(day.LunchInMinutes);
        Assert.Equal("20:44", TimesheetAggregationService.FormatClock(day.ClockOutMinutes!.Value));
        Assert.Equal("Incompleto", day.Status);
    }

    [Fact]
    public void MergeService_ExcludesFutureDaysFromEntries()
    {
        var mergeService = new TimesheetMergeService(_service);
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            BrazilTimeZone.SaoPauloTimeZone).Date;
        var dataDe = new DateTime(todayLocal.Year, todayLocal.Month, 1);
        var dataAte = dataDe.AddMonths(1).AddDays(-1);

        var (_, entries) = mergeService.Merge(dataDe, dataAte, [], []);

        Assert.All(entries, entry => Assert.True(entry.Date.Date <= todayLocal));
        Assert.Equal(todayLocal.Day, entries.Count);
    }

    [Fact]
    public void MergeService_UsesProcessedDayTotalsWhenAvailable()
    {
        var mergeService = new TimesheetMergeService(_service);
        var dataDe = new DateTime(2026, 6, 1);
        var dataAte = new DateTime(2026, 6, 1);

        var punches = new List<RmPunchRecord>
        {
            new() { DataPonto = new DateTime(2026, 6, 1), BatidaMinutos = 480, Natureza = 0 },
            new() { DataPonto = new DateTime(2026, 6, 1), BatidaMinutos = 1020, Natureza = 2 }
        };

        var processed = new List<RmProcessedDayRecord>
        {
            new()
            {
                DataPonto = new DateTime(2026, 6, 1),
                WorkedMinutes = 480,
                ExpectedMinutes = 480,
                BalanceMinutes = 0,
                DelayMinutes = 0,
                AbsenceMinutes = 0,
                StatusCode = "D"
            }
        };

        var (summary, entries) = mergeService.Merge(dataDe, dataAte, punches, processed);

        Assert.Single(entries);
        Assert.Equal("8h00", entries[0].WorkedHours);
        Assert.Equal("0h00", entries[0].BalanceHours);
        Assert.Equal("Regular", entries[0].Status);
        Assert.Equal("8h00", summary.WorkedHours);
    }
}
