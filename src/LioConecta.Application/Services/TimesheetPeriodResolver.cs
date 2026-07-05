using LioConecta.Application.Common;

namespace LioConecta.Application.Services;

public static class TimesheetPeriodResolver
{
    public const int DefaultStartDay = 16;
    public const int DefaultEndDay = 15;
    public const int DefaultRecentPeriodCount = 12;

    public static (DateTime DataDe, DateTime DataAte, int EndMonth, int EndYear) Resolve(
        int? endMonth,
        int? endYear,
        int periodStartDay,
        int periodEndDay,
        DateTime? referenceToday = null)
    {
        var startDay = NormalizeDay(periodStartDay, DefaultStartDay);
        var endDay = NormalizeDay(periodEndDay, DefaultEndDay);
        var today = referenceToday
            ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone.SaoPauloTimeZone).Date;

        var currentPeriodEnd = ResolveCurrentPeriodEnd(today, endDay);
        var resolvedEndMonth = endMonth is >= 1 and <= 12 ? endMonth.Value : currentPeriodEnd.EndMonth;
        var resolvedEndYear = endYear is >= 2000 and <= 2100 ? endYear.Value : currentPeriodEnd.EndYear;

        var dataAte = BuildPeriodEnd(resolvedEndYear, resolvedEndMonth, endDay);
        var dataDe = BuildPeriodStart(dataAte, startDay);

        return (dataDe, dataAte, resolvedEndMonth, resolvedEndYear);
    }

    public static (int EndMonth, int EndYear) ResolveCurrentPeriodEnd(DateTime today, int periodEndDay)
    {
        var endDay = NormalizeDay(periodEndDay, DefaultEndDay);
        if (today.Day > endDay)
        {
            var nextMonth = today.AddMonths(1);
            return (nextMonth.Month, nextMonth.Year);
        }

        return (today.Month, today.Year);
    }

    public static IReadOnlyList<TimesheetPeriodOption> BuildRecentPeriodOptions(
        int count,
        int periodStartDay,
        int periodEndDay,
        DateTime? referenceToday = null)
    {
        var startDay = NormalizeDay(periodStartDay, DefaultStartDay);
        var endDay = NormalizeDay(periodEndDay, DefaultEndDay);
        var today = referenceToday
            ?? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone.SaoPauloTimeZone).Date;
        var (currentEndMonth, currentEndYear) = ResolveCurrentPeriodEnd(today, endDay);

        var options = new List<TimesheetPeriodOption>(count);
        var cursorMonth = currentEndMonth;
        var cursorYear = currentEndYear;

        for (var index = 0; index < count; index++)
        {
            var dataAte = BuildPeriodEnd(cursorYear, cursorMonth, endDay);
            var dataDe = BuildPeriodStart(dataAte, startDay);
            options.Add(new TimesheetPeriodOption(cursorMonth, cursorYear, FormatPeriodLabel(dataDe, dataAte)));

            if (cursorMonth == 1)
            {
                cursorMonth = 12;
                cursorYear--;
            }
            else
            {
                cursorMonth--;
            }
        }

        return options;
    }

    public static string FormatPeriodLabel(DateTime dataDe, DateTime dataAte) =>
        $"{dataDe:dd/MM/yyyy} - {dataAte:dd/MM/yyyy}";

    public static int NormalizeDay(int value, int fallback) =>
        value is >= 1 and <= 28 ? value : fallback;

    private static DateTime BuildPeriodEnd(int endYear, int endMonth, int endDay)
    {
        var safeDay = Math.Min(NormalizeDay(endDay, DefaultEndDay), DateTime.DaysInMonth(endYear, endMonth));
        return new DateTime(endYear, endMonth, safeDay);
    }

    private static DateTime BuildPeriodStart(DateTime periodEnd, int startDay)
    {
        var startMonthDate = periodEnd.AddMonths(-1);
        var safeDay = Math.Min(
            NormalizeDay(startDay, DefaultStartDay),
            DateTime.DaysInMonth(startMonthDate.Year, startMonthDate.Month));
        return new DateTime(startMonthDate.Year, startMonthDate.Month, safeDay);
    }
}

public sealed record TimesheetPeriodOption(int EndMonth, int EndYear, string Label);
