namespace LioConecta.Application.DTOs;

public sealed record TotvsRmConfigurationDto(
    Guid Id,
    bool IsEnabled,
    string Server,
    int Port,
    string Database,
    string UserName,
    bool HasPassword,
    bool TrustServerCertificate,
    int TimesheetPeriodStartDay,
    int TimesheetPeriodEndDay,
    DateTimeOffset UpdatedAt);

public sealed record UpsertTotvsRmConfigurationRequest(
    bool IsEnabled,
    string Server,
    int Port,
    string Database,
    string UserName,
    string? Password,
    bool TrustServerCertificate,
    int TimesheetPeriodStartDay,
    int TimesheetPeriodEndDay);

public sealed record TotvsRmConnectionTestResponse(
    bool Success,
    string Message,
    string? Detail);

public sealed record TotvsRmRuntimeConfiguration(
    bool IsEnabled,
    string Server,
    int Port,
    string Database,
    string UserName,
    string? Password,
    bool TrustServerCertificate,
    int TimesheetPeriodStartDay,
    int TimesheetPeriodEndDay);

public sealed record PontoPeriodOptionDto(int EndMonth, int EndYear, string Label);

public sealed record PontoPeriodSettingsDto(
    int TimesheetPeriodStartDay,
    int TimesheetPeriodEndDay,
    IReadOnlyList<PontoPeriodOptionDto> Options);
