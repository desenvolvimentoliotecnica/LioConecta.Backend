namespace LioConecta.Application.DTOs;

public sealed record PontoEntryDto(
    DateTime Date,
    string WeekdayLabel,
    string ClockIn,
    string LunchOut,
    string LunchIn,
    string ClockOut,
    string BreakMinutes,
    string WorkedHours,
    string BalanceHours,
    string Status);

public sealed record PontoSummaryDto(
    string PeriodLabel,
    string WorkedHours,
    string ExpectedHours,
    string BalanceHours,
    int Absences,
    int Delays);

public sealed record PontoResponseDto(
    string Title,
    PontoSummaryDto? Summary,
    IReadOnlyList<PontoEntryDto> Entries,
    string Provider,
    bool IsSimulated,
    string? AvailabilityStatus,
    string? UserMessage,
    string? DataSource,
    DateTimeOffset? SyncedAt);
