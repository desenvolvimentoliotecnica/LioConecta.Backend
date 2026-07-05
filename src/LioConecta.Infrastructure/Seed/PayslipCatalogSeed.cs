using System.Text.Json;
using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Seed;

internal static class PayslipCatalogSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static IReadOnlyList<Payslip> BuildPayslips(Guid personId, DateTimeOffset seedTime)
    {
        var payslips = new List<Payslip>();
        var start = new DateOnly(2024, 7, 1);
        var end = new DateOnly(2026, 6, 1);

        for (var date = start; date <= end; date = date.AddMonths(1))
        {
            var index = ((date.Year - 2024) * 12) + date.Month - 7;
            var baseSalary = 12500m;
            var overtime = index % 3 == 0 ? 850m : 0m;
            var bonus = date.Month == 12 ? 2500m : 0m;
            var gross = baseSalary + overtime + bonus;
            var inss = Math.Round(gross * 0.11m, 2);
            var irrf = Math.Round(gross * 0.15m, 2);
            var health = 420m;
            var vt = 198m;
            var deductions = inss + irrf + health + vt;
            var net = date.Year == 2026 && date.Month == 6
                ? 8742.50m
                : Math.Round(gross - deductions, 2);

            if (date.Year == 2026 && date.Month == 6)
            {
                gross = 12480m;
                deductions = gross - net;
            }

            var earnings = new[]
            {
                new { code = "001", label = "Salário base", amount = baseSalary, quantity = (decimal?)null },
                overtime > 0
                    ? new { code = "050", label = "Horas extras 50%", amount = overtime, quantity = (decimal?)12m }
                    : new { code = "050", label = "Horas extras 50%", amount = 0m, quantity = (decimal?)null },
                bonus > 0
                    ? new { code = "080", label = "13º salário proporcional", amount = bonus, quantity = (decimal?)null }
                    : new { code = "080", label = "13º salário proporcional", amount = 0m, quantity = (decimal?)null }
            }.Where(e => e.amount > 0).ToArray();

            var deductionLines = new[]
            {
                new { code = "201", label = "INSS", amount = inss, quantity = (decimal?)null },
                new { code = "202", label = "IRRF", amount = irrf, quantity = (decimal?)null },
                new { code = "210", label = "Plano de saúde", amount = health, quantity = (decimal?)null },
                new { code = "220", label = "Vale-transporte", amount = vt, quantity = (decimal?)null }
            };

            payslips.Add(new Payslip
            {
                Id = Guid.Parse($"ffffffff-0001-0001-{date.Year:D4}-{date.Month:D12}"),
                PersonId = personId,
                Year = date.Year,
                Month = date.Month,
                GrossAmount = gross,
                NetAmount = net,
                DeductionsTotal = deductions,
                EarningsJson = JsonSerializer.Serialize(earnings, JsonOptions),
                DeductionsJson = JsonSerializer.Serialize(deductionLines, JsonOptions),
                PublishedAt = new DateTimeOffset(date.Year, date.Month, 5, 12, 0, 0, TimeSpan.Zero),
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            });
        }

        return payslips;
    }

    internal static IncomeStatement BuildIncomeStatement2025(Guid personId, DateTimeOffset seedTime)
    {
        var lines = Enumerable.Range(1, 12)
            .Select(month => new
            {
                month,
                paid = month == 12 ? 14980m : 12480m,
                withheld = month == 12 ? 3120m : 2480m
            })
            .ToArray();

        return new IncomeStatement
        {
            Id = Guid.Parse("ffffffff-0002-0001-2025-000000000001"),
            PersonId = personId,
            Year = 2025,
            TotalPaid = lines.Sum(l => l.paid),
            TotalWithheld = lines.Sum(l => l.withheld),
            LinesJson = JsonSerializer.Serialize(lines, JsonOptions),
            CreatedAt = seedTime,
            UpdatedAt = seedTime
        };
    }
}
