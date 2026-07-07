using System.Globalization;
using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Mapping;

public static class PersonRmCareerHistoryBuilder
{
    private static readonly string[] MonthLabels =
    [
        "", "jan", "fev", "mar", "abr", "mai", "jun",
        "jul", "ago", "set", "out", "nov", "dez",
    ];

    public static List<Dictionary<string, object?>> Build(
        RmEmployeeProfileRecord rm,
        RmEmployeeCareerHistoryData historyData,
        bool includeSalaryValues = false)
    {
        var events = new List<CareerTimelineEvent>();
        var admissionDate = rm.DataAdmissao?.Date;
        var functionHistory = historyData.FunctionHistory
            .OrderBy(item => item.EventDate)
            .ToList();
        var sectionHistory = historyData.SectionHistory
            .OrderBy(item => item.EventDate)
            .ToList();
        var salaryHistory = historyData.SalaryHistory
            .OrderBy(item => item.EventDate)
            .ToList();

        var admissionFunction = ResolveAdmissionFunction(rm, functionHistory);
        var admissionSection = ResolveAdmissionSection(rm, sectionHistory);

        if (admissionDate.HasValue)
        {
            events.Add(new CareerTimelineEvent(
                admissionDate.Value,
                "admission",
                admissionFunction,
                admissionSection,
                "Admissão registrada no TOTVS RM.",
                Priority: 1));
        }

        var previousFunctionCode = functionHistory.FirstOrDefault()?.CodFuncao;
        foreach (var row in functionHistory)
        {
            if (admissionDate.HasValue && SameDay(row.EventDate, admissionDate.Value))
            {
                previousFunctionCode = row.CodFuncao;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(previousFunctionCode)
                && !string.Equals(previousFunctionCode, row.CodFuncao, StringComparison.OrdinalIgnoreCase))
            {
                events.Add(new CareerTimelineEvent(
                    row.EventDate.Date,
                    "promotion",
                    ResolveFunctionTitle(row),
                    ResolveSectionAtDate(sectionHistory, row.EventDate) ?? admissionSection,
                    "Alteração de função registrada no TOTVS RM.",
                    Priority: 3));
            }

            previousFunctionCode = row.CodFuncao;
        }

        var functionDates = functionHistory
            .Select(item => item.EventDate.Date)
            .ToHashSet();
        var previousSectionCode = sectionHistory.FirstOrDefault()?.CodSecao;
        foreach (var row in sectionHistory)
        {
            if (admissionDate.HasValue && SameDay(row.EventDate, admissionDate.Value))
            {
                previousSectionCode = row.CodSecao;
                continue;
            }

            if (functionDates.Contains(row.EventDate.Date))
            {
                previousSectionCode = row.CodSecao;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(previousSectionCode)
                && !string.Equals(previousSectionCode, row.CodSecao, StringComparison.OrdinalIgnoreCase))
            {
                events.Add(new CareerTimelineEvent(
                    row.EventDate.Date,
                    "transfer",
                    ResolveFunctionAtDate(functionHistory, row.EventDate) ?? rm.FuncaoDescricao ?? string.Empty,
                    row.SecaoDescricao ?? string.Empty,
                    "Transferência de seção registrada no TOTVS RM.",
                    Priority: 2));
            }

            previousSectionCode = row.CodSecao;
        }

        var busyDates = events
            .Select(item => item.Date)
            .ToHashSet();
        decimal? previousSalary = null;
        foreach (var row in salaryHistory)
        {
            var date = row.EventDate.Date;
            if (busyDates.Contains(date))
            {
                previousSalary = row.Salario;
                continue;
            }

            if (previousSalary.HasValue
                && row.Salario.HasValue
                && previousSalary.Value == row.Salario.Value)
            {
                continue;
            }

            var note = string.IsNullOrWhiteSpace(row.Motivo)
                ? "Ajuste salarial registrado no TOTVS RM."
                : $"Ajuste salarial no TOTVS RM: {row.Motivo.Trim()}.";
            if (includeSalaryValues && row.Salario.HasValue)
            {
                var salaryText = row.Salario.Value.ToString("C", new CultureInfo("pt-BR"));
                note = $"{note} Valor: {salaryText}.";
            }

            events.Add(new CareerTimelineEvent(
                date,
                "salary",
                rm.FuncaoDescricao ?? string.Empty,
                rm.SecaoDescricao ?? string.Empty,
                note,
                Priority: 0));

            busyDates.Add(date);
            previousSalary = row.Salario;
        }

        events = DeduplicateSameDay(events);

        if (events.Count == 0 && admissionDate.HasValue)
        {
            events.Add(new CareerTimelineEvent(
                admissionDate.Value,
                "admission",
                rm.FuncaoDescricao ?? string.Empty,
                rm.SecaoDescricao ?? string.Empty,
                "Admissão registrada no TOTVS RM.",
                Priority: 1));
        }

        AddCurrentPosition(events, rm);

        return events
            .OrderByDescending(item => item.Date)
            .ThenByDescending(item => item.Priority)
            .Select(ToDictionary)
            .ToList();
    }

    private static void AddCurrentPosition(
        List<CareerTimelineEvent> events,
        RmEmployeeProfileRecord rm)
    {
        events.RemoveAll(item => item.Type == "atual");

        if (string.IsNullOrWhiteSpace(rm.FuncaoDescricao) && string.IsNullOrWhiteSpace(rm.SecaoDescricao))
        {
            return;
        }

        events.Add(new CareerTimelineEvent(
            DateTime.UtcNow.Date,
            "atual",
            rm.FuncaoDescricao ?? string.Empty,
            rm.SecaoDescricao ?? string.Empty,
            "Cargo atual na LioConecta.",
            Priority: 5));
    }

    private static List<CareerTimelineEvent> DeduplicateSameDay(List<CareerTimelineEvent> events)
    {
        return events
            .Where(item => item.Type != "atual")
            .GroupBy(item => item.Date)
            .Select(group => group.OrderByDescending(item => item.Priority).First())
            .Concat(events.Where(item => item.Type == "atual"))
            .ToList();
    }

    private static string ResolveAdmissionFunction(
        RmEmployeeProfileRecord rm,
        IReadOnlyList<RmEmployeeFunctionHistoryRecord> functionHistory)
    {
        if (functionHistory.Count > 0)
        {
            return ResolveFunctionTitle(functionHistory[0]);
        }

        return rm.FuncaoDescricao ?? string.Empty;
    }

    private static string ResolveAdmissionSection(
        RmEmployeeProfileRecord rm,
        IReadOnlyList<RmEmployeeSectionHistoryRecord> sectionHistory)
    {
        if (sectionHistory.Count > 0)
        {
            return sectionHistory[0].SecaoDescricao ?? string.Empty;
        }

        return rm.SecaoDescricao ?? string.Empty;
    }

    private static string ResolveFunctionTitle(RmEmployeeFunctionHistoryRecord row)
    {
        if (!string.IsNullOrWhiteSpace(row.FuncaoDescricao))
        {
            return row.FuncaoDescricao.Trim();
        }

        return row.CargoDescricao?.Trim() ?? string.Empty;
    }

    private static string? ResolveSectionAtDate(
        IReadOnlyList<RmEmployeeSectionHistoryRecord> sectionHistory,
        DateTime date)
    {
        return sectionHistory
            .Where(item => item.EventDate.Date <= date.Date)
            .OrderByDescending(item => item.EventDate)
            .Select(item => item.SecaoDescricao)
            .FirstOrDefault(section => !string.IsNullOrWhiteSpace(section));
    }

    private static string? ResolveFunctionAtDate(
        IReadOnlyList<RmEmployeeFunctionHistoryRecord> functionHistory,
        DateTime date)
    {
        return functionHistory
            .Where(item => item.EventDate.Date <= date.Date)
            .OrderByDescending(item => item.EventDate)
            .Select(ResolveFunctionTitle)
            .FirstOrDefault(title => !string.IsNullOrWhiteSpace(title));
    }

    private static bool SameDay(DateTime left, DateTime right)
        => left.Date == right.Date;

    private static Dictionary<string, object?> ToDictionary(CareerTimelineEvent item)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = item.Type,
            ["title"] = item.Title,
            ["date"] = FormatTimelineDate(item.Date),
            ["dept"] = item.Department,
            ["note"] = item.Note,
        };

    public static string FormatTimelineDate(DateTime date)
    {
        if (date.Month is >= 1 and <= 12)
        {
            return $"{MonthLabels[date.Month]} de {date.Year}";
        }

        return date.Year.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record CareerTimelineEvent(
        DateTime Date,
        string Type,
        string Title,
        string Department,
        string Note,
        int Priority);
}
