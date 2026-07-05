using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations.Models;
using System.Globalization;

namespace LioConecta.Application.Services;

public sealed class TimesheetMergeService(TimesheetAggregationService aggregationService)
{
    public (PontoSummaryDto Summary, IReadOnlyList<PontoEntryDto> Entries) Merge(
        DateTime dataDe,
        DateTime dataAte,
        IReadOnlyList<RmPunchRecord> punches,
        IReadOnlyList<RmProcessedDayRecord> processedDays)
    {
        var aggregated = aggregationService.AggregateByDay(punches);
        var processedByDate = processedDays.ToDictionary(item => item.DataPonto.Date);

        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTimeZone.SaoPauloTimeZone).Date;
        var effectiveEnd = dataAte.Date > todayLocal ? todayLocal : dataAte.Date;
        if (effectiveEnd < dataDe.Date)
        {
            effectiveEnd = dataDe.Date;
        }

        var dates = Enumerable
            .Range(0, (effectiveEnd - dataDe.Date).Days + 1)
            .Select(offset => dataDe.Date.AddDays(offset))
            .OrderByDescending(item => item)
            .ToList();

        var entries = dates
            .Select(date => BuildEntry(date, aggregated, processedByDate))
            .ToList();

        var summary = BuildSummary(dataDe, dataAte, entries, processedDays);
        return (summary, entries);
    }

    private PontoEntryDto BuildEntry(
        DateTime date,
        IReadOnlyDictionary<DateTime, AggregatedDayPunch> aggregated,
        IReadOnlyDictionary<DateTime, RmProcessedDayRecord> processedByDate)
    {
        aggregated.TryGetValue(date, out var dayPunch);
        processedByDate.TryGetValue(date, out var processed);

        var clockIn = FormatPunch(dayPunch?.ClockInMinutes);
        var lunchOut = FormatPunch(dayPunch?.LunchOutMinutes);
        var lunchIn = FormatPunch(dayPunch?.LunchInMinutes);
        var clockOut = FormatPunch(dayPunch?.ClockOutMinutes);

        var breakMinutes = dayPunch?.BreakMinutes ?? 0;
        var workedMinutes = processed?.WorkedMinutes ?? dayPunch?.WorkedMinutes ?? 0;
        var expectedMinutes = processed?.ExpectedMinutes;
        var balanceMinutes = processed?.BalanceMinutes
            ?? (expectedMinutes.HasValue ? workedMinutes - expectedMinutes.Value : (int?)null);

        var status = ResolveStatus(processed, dayPunch);

        return new PontoEntryDto(
            date,
            TimesheetAggregationService.GetWeekdayLabel(date),
            clockIn,
            lunchOut,
            lunchIn,
            clockOut,
            breakMinutes.ToString(CultureInfo.InvariantCulture),
            TimesheetAggregationService.FormatMinutes(workedMinutes),
            balanceMinutes.HasValue
                ? FormatSignedMinutes(balanceMinutes.Value)
                : "—",
            status);
    }

    private static string FormatPunch(int? minutes)
    {
        return minutes is int value
            ? TimesheetAggregationService.FormatClock(value)
            : "—";
    }

    private static string ResolveStatus(RmProcessedDayRecord? processed, AggregatedDayPunch? dayPunch)
    {
        if (!string.IsNullOrWhiteSpace(processed?.StatusCode))
        {
            return MapRmStatus(processed.StatusCode);
        }

        return dayPunch?.Status ?? "Sem registro";
    }

    private static string MapRmStatus(string statusCode)
    {
        return statusCode.Trim().ToUpperInvariant() switch
        {
            "F" => "Falta",
            "A" => "Atraso",
            "D" => "Regular",
            _ => statusCode
        };
    }

    private PontoSummaryDto BuildSummary(
        DateTime dataDe,
        DateTime dataAte,
        IReadOnlyList<PontoEntryDto> entries,
        IReadOnlyList<RmProcessedDayRecord> processedDays)
    {
        var periodLabel = dataDe.Month == dataAte.Month && dataDe.Year == dataAte.Year
            ? dataDe.ToString("MMMM/yyyy", CultureInfo.GetCultureInfo("pt-BR"))
            : $"{dataDe:dd/MM/yyyy} - {dataAte:dd/MM/yyyy}";

        var workedMinutes = processedDays.Sum(item => item.WorkedMinutes ?? 0);
        if (workedMinutes == 0)
        {
            workedMinutes = entries.Sum(item => ParseWorkedMinutes(item.WorkedHours));
        }

        var expectedMinutes = processedDays.Sum(item => item.ExpectedMinutes ?? 0);
        var balanceMinutes = processedDays.Any(item => item.BalanceMinutes.HasValue)
            ? processedDays.Sum(item => item.BalanceMinutes ?? 0)
            : expectedMinutes > 0
                ? workedMinutes - expectedMinutes
                : 0;

        var absences = processedDays.Count(item => (item.AbsenceMinutes ?? 0) > 0
            || string.Equals(item.StatusCode, "F", StringComparison.OrdinalIgnoreCase));
        var delays = processedDays.Count(item => (item.DelayMinutes ?? 0) > 0
            || string.Equals(item.StatusCode, "A", StringComparison.OrdinalIgnoreCase));

        return new PontoSummaryDto(
            CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(periodLabel),
            TimesheetAggregationService.FormatMinutes(workedMinutes),
            expectedMinutes > 0 ? TimesheetAggregationService.FormatMinutes(expectedMinutes) : "—",
            FormatSignedMinutes(balanceMinutes),
            absences,
            delays);
    }

    private static int ParseWorkedMinutes(string workedHours)
    {
        if (string.IsNullOrWhiteSpace(workedHours) || workedHours == "—")
        {
            return 0;
        }

        var normalized = workedHours.Trim().ToLowerInvariant();
        var hoursPart = 0;
        var minutesPart = 0;

        var hourIndex = normalized.IndexOf('h', StringComparison.Ordinal);
        if (hourIndex >= 0)
        {
            _ = int.TryParse(normalized[..hourIndex], out hoursPart);
            var minuteSlice = normalized[(hourIndex + 1)..];
            _ = int.TryParse(minuteSlice, out minutesPart);
        }

        return (hoursPart * 60) + minutesPart;
    }

    private static string FormatSignedMinutes(int minutes)
    {
        var formatted = TimesheetAggregationService.FormatMinutes(minutes);
        return minutes > 0 ? $"+{formatted}" : formatted;
    }
}
