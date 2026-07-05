using LioConecta.Application.Interfaces.Integrations.Models;
using System.Globalization;

namespace LioConecta.Application.Services;

public sealed class TimesheetAggregationService
{
    public IReadOnlyDictionary<DateTime, AggregatedDayPunch> AggregateByDay(IEnumerable<RmPunchRecord> punches)
    {
        return punches
            .GroupBy(item => item.DataPonto.Date)
            .ToDictionary(
                group => group.Key,
                group => AggregateDay(group.Key, group.OrderBy(item => item.BatidaMinutos).ToList()));
    }

    private static AggregatedDayPunch AggregateDay(DateTime date, IReadOnlyList<RmPunchRecord> dayPunches)
    {
        if (dayPunches.Count == 0)
        {
            return new AggregatedDayPunch(date, null, null, null, null, 0, 0, "Sem registro");
        }

        var ordered = dayPunches
            .Select(item => item.BatidaMinutos)
            .OrderBy(item => item)
            .ToList();

        int? clockIn = ordered.Count >= 1 ? ordered[0] : null;
        int? lunchOut = null;
        int? lunchIn = null;
        int? clockOut = null;

        switch (ordered.Count)
        {
            case >= 4:
                lunchOut = ordered[1];
                lunchIn = ordered[2];
                clockOut = ordered[3];
                break;
            case 3:
                lunchOut = ordered[1];
                lunchIn = ordered[2];
                break;
            case 2:
                clockOut = ordered[1];
                break;
        }

        var firstPunch = ordered[0];
        var lastPunch = ordered[^1];
        var breakMinutes = ordered.Count >= 4
            ? CalculateBreakMinutes(ordered, firstPunch, lastPunch)
            : 0;
        var workedMinutes = ordered.Count >= 2
            ? Math.Max(0, lastPunch - firstPunch - breakMinutes)
            : 0;
        var status = ordered.Count >= 4 && lastPunch > firstPunch
            ? "Regular"
            : ordered.Count >= 2 && lastPunch > firstPunch
                ? "Incompleto"
                : "Incompleto";

        return new AggregatedDayPunch(
            date,
            clockIn,
            lunchOut,
            lunchIn,
            clockOut,
            breakMinutes,
            workedMinutes,
            status);
    }

    private static int CalculateBreakMinutes(IReadOnlyList<int> ordered, int clockIn, int clockOut)
    {
        if (ordered.Count < 4)
        {
            return 0;
        }

        var breakMinutes = 0;
        for (var index = 1; index < ordered.Count - 1; index += 2)
        {
            var exitMinute = ordered[index];
            var nextEntryMinute = ordered[index + 1];
            if (exitMinute >= clockIn && nextEntryMinute <= clockOut && nextEntryMinute > exitMinute)
            {
                breakMinutes += nextEntryMinute - exitMinute;
            }
        }

        return breakMinutes;
    }

    public static string FormatMinutes(int minutes)
    {
        var absolute = Math.Abs(minutes);
        var hours = absolute / 60;
        var mins = absolute % 60;
        var formatted = $"{hours}h{mins:D2}";
        return minutes < 0 ? $"-{formatted}" : formatted;
    }

    public static string FormatClock(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return $"{hours:D2}:{mins:D2}";
    }

    public static string GetWeekdayLabel(DateTime date)
    {
        return CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetDayName(date.DayOfWeek);
    }
}

public sealed record AggregatedDayPunch(
    DateTime Date,
    int? ClockInMinutes,
    int? LunchOutMinutes,
    int? LunchInMinutes,
    int? ClockOutMinutes,
    int BreakMinutes,
    int WorkedMinutes,
    string Status);
