namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed record RmLeavePeriodRecord(
    DateOnly? InicioPeriodo,
    DateOnly? FimPeriodo,
    int SaldoDias,
    int DiasAdquiridos,
    int DiasUsados,
    DateOnly? DataVencimento);

public sealed record RmVacationRequestRecord(
    string ExternalId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Days,
    string? RmStatus,
    string? Title);

public sealed record RmLeaveBalanceData(
    int AvailableDays,
    int AcquiredDays,
    int ScheduledDays,
    int ExpiredDays,
    DateOnly? NextScheduledStart,
    DateOnly? NextScheduledEnd,
    IReadOnlyList<RmLeavePeriodRecord> Periods,
    IReadOnlyList<RmVacationRequestRecord> Requests);
