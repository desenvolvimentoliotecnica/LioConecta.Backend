using System.Globalization;
using System.Text.Json;
using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Seed;

internal sealed record LeavePeriodSeed(
    string Label,
    int AcquiredDays,
    int UsedDays,
    int AvailableDays,
    DateOnly? ExpiresAt);

public static class LeaveCatalogSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new();
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static EmployeeLeaveBalance BuildBalanceForPerson(Guid personId, DateTimeOffset seedTime)
    {
        var periods = new[]
        {
            new LeavePeriodSeed("2024/2025", 30, 12, 18, new DateOnly(2026, 12, 31)),
            new LeavePeriodSeed("2025/2026", 30, 0, 30, new DateOnly(2027, 12, 31)),
        };

        return new EmployeeLeaveBalance
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            AvailableDays = 18,
            AcquiredDays = 30,
            ScheduledDays = 12,
            ExpiredDays = 0,
            BancoHorasBalanceHours = 12.5m,
            NextScheduledStart = new DateOnly(2026, 8, 10),
            NextScheduledEnd = new DateOnly(2026, 8, 24),
            BreakdownJson = JsonSerializer.Serialize(new
            {
                periods = periods.Select(p => new
                {
                    label = p.Label,
                    acquiredDays = p.AcquiredDays,
                    usedDays = p.UsedDays,
                    availableDays = p.AvailableDays,
                    expiresAt = p.ExpiresAt?.ToString("O"),
                }),
                notes = new[]
                {
                    "Período aquisitivo 2024/2025 com 18 dias disponíveis para programação.",
                    "Férias coletivas de dez/2026 já contabilizadas em dias programados.",
                },
            }, JsonOptions),
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };
    }

    public static IReadOnlyList<LeaveRecord> BuildRecordsForPerson(Guid personId, DateTimeOffset seedTime)
    {
        return
        [
            PendingVacationRequest(personId, seedTime),
            ApprovedVacation(personId, seedTime),
            PastLicense(personId, seedTime),
            PastMedicalLeave(personId, seedTime),
            PastVacation(personId, seedTime),
            BancoHorasCompensation(personId, seedTime),
        ];
    }

    private static LeaveRecord PendingVacationRequest(Guid personId, DateTimeOffset seedTime) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = "solicitar-ferias",
            RecordType = "ferias",
            Title = "Férias — Dez/2026 (solicitação)",
            Status = "pending",
            StartDate = new DateOnly(2026, 12, 14),
            EndDate = new DateOnly(2026, 12, 28),
            Days = 15,
            DetailsJson = JsonSerializer.Serialize(new
            {
                note = "Aguardando aprovação do gestor Ricardo Souza.",
                substitute = "Julia Santos",
            }, JsonOptions),
            CreatedAt = seedTime.AddDays(-3),
            UpdatedAt = seedTime.AddDays(-3),
        };

    private static LeaveRecord ApprovedVacation(Guid personId, DateTimeOffset seedTime) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = "solicitar-ferias",
            RecordType = "ferias",
            Title = "Férias — Ago/2026",
            Status = "approved",
            StartDate = new DateOnly(2026, 8, 10),
            EndDate = new DateOnly(2026, 8, 24),
            Days = 12,
            DetailsJson = JsonSerializer.Serialize(new { note = "Aprovado em 15/05/2026." }, JsonOptions),
            CreatedAt = seedTime.AddMonths(-2),
            UpdatedAt = seedTime.AddMonths(-2),
        };

    private static LeaveRecord PastVacation(Guid personId, DateTimeOffset seedTime) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = "solicitar-ferias",
            RecordType = "ferias",
            Title = "Férias — Jan/2026",
            Status = "completed",
            StartDate = new DateOnly(2026, 1, 6),
            EndDate = new DateOnly(2026, 1, 17),
            Days = 10,
            DetailsJson = "{}",
            CreatedAt = seedTime.AddMonths(-6),
            UpdatedAt = seedTime.AddMonths(-6),
        };

    private static LeaveRecord PastLicense(Guid personId, DateTimeOffset seedTime) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = "lic-gala",
            RecordType = "licenca",
            Title = "Licença gala",
            Status = "completed",
            StartDate = new DateOnly(2025, 11, 3),
            EndDate = new DateOnly(2025, 11, 5),
            Days = 3,
            DetailsJson = JsonSerializer.Serialize(new { note = "Casamento — comprovante arquivado." }, JsonOptions),
            CreatedAt = seedTime.AddMonths(-8),
            UpdatedAt = seedTime.AddMonths(-8),
        };

    private static LeaveRecord PastMedicalLeave(Guid personId, DateTimeOffset seedTime) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = "atestado",
            RecordType = "afastamento",
            Title = "Atestado médico",
            Status = "completed",
            StartDate = new DateOnly(2025, 9, 12),
            EndDate = new DateOnly(2025, 9, 13),
            Days = 2,
            DetailsJson = JsonSerializer.Serialize(new { note = "CID registrado no ponto." }, JsonOptions),
            CreatedAt = seedTime.AddMonths(-9),
            UpdatedAt = seedTime.AddMonths(-9),
        };

    private static LeaveRecord BancoHorasCompensation(Guid personId, DateTimeOffset seedTime) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            ServiceKey = "banco-horas",
            RecordType = "banco",
            Title = "Compensação banco de horas",
            Status = "completed",
            StartDate = new DateOnly(2026, 5, 30),
            EndDate = new DateOnly(2026, 5, 30),
            Days = 1,
            DetailsJson = JsonSerializer.Serialize(new { hours = -8m, note = "Saída antecipada compensada." }, JsonOptions),
            CreatedAt = seedTime.AddMonths(-1),
            UpdatedAt = seedTime.AddMonths(-1),
        };

    public static string FormatScheduledLabel(DateOnly? start, DateOnly? end)
    {
        if (start is null)
        {
            return "—";
        }

        var month = PtBr.DateTimeFormat.GetAbbreviatedMonthName(start.Value.Month);
        month = char.ToUpper(month[0]) + month[1..];
        return end is null
            ? $"{month}/{start.Value.Year}"
            : $"{month}/{start.Value.Year}";
    }
}
