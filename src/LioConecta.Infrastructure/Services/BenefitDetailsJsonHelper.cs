using System.Text.Json;
using LioConecta.Application.DTOs;

namespace LioConecta.Infrastructure.Services;

internal static class BenefitDetailsJsonHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string Serialize(
        IReadOnlyList<BenefitDetailLineDto>? lines,
        IReadOnlyList<BenefitDependentDto>? dependents,
        IReadOnlyList<string>? notes) =>
        JsonSerializer.Serialize(new
        {
            lines = (lines ?? []).Select(line => new { label = line.Label, amount = line.Amount, note = line.Note }),
            dependents = (dependents ?? []).Select(dep => new
            {
                name = dep.Name,
                relation = dep.Relation,
                monthlyValue = dep.MonthlyValue,
            }),
            notes = notes ?? [],
        }, JsonOptions);

    public static (IReadOnlyList<BenefitDetailLineDto> Lines, IReadOnlyList<BenefitDependentDto> Dependents, IReadOnlyList<string> Notes) Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ([], [], []);
        }

        var raw = JsonSerializer.Deserialize<BenefitDetailsRaw>(json, JsonOptions);
        if (raw is null)
        {
            return ([], [], []);
        }

        var lines = raw.Lines?
            .Select(line => new BenefitDetailLineDto(line.Label ?? string.Empty, line.Amount, line.Note))
            .ToList() ?? [];

        var dependents = raw.Dependents?
            .Select(dep => new BenefitDependentDto(dep.Name ?? string.Empty, dep.Relation ?? string.Empty, dep.MonthlyValue))
            .ToList() ?? [];

        return (lines, dependents, raw.Notes ?? []);
    }

    private sealed class BenefitDetailsRaw
    {
        public List<BenefitLineRaw>? Lines { get; set; }
        public List<BenefitDependentRaw>? Dependents { get; set; }
        public List<string>? Notes { get; set; }
    }

    private sealed class BenefitLineRaw
    {
        public string? Label { get; set; }
        public decimal? Amount { get; set; }
        public string? Note { get; set; }
    }

    private sealed class BenefitDependentRaw
    {
        public string? Name { get; set; }
        public string? Relation { get; set; }
        public decimal? MonthlyValue { get; set; }
    }
}
