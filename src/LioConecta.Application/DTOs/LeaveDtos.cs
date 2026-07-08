namespace LioConecta.Application.DTOs;

public sealed record LeaveSummaryDto(
    int AvailableDays,
    int PendingRequests,
    string? NextScheduledLabel);

public sealed record LeaveServiceDto(
    string Id,
    string Title,
    string Desc,
    string Category,
    string Sla,
    bool Online,
    bool Featured,
    string Action,
    string HelpText,
    string? PortalUrl);

public sealed record LeavePeriodDto(
    string Label,
    int AcquiredDays,
    int UsedDays,
    int AvailableDays,
    DateOnly? ExpiresAt);

public sealed record LeaveBalanceDto(
    int AvailableDays,
    int AcquiredDays,
    int ScheduledDays,
    int ExpiredDays,
    IReadOnlyList<LeavePeriodDto> Periods,
    IReadOnlyList<string> Notes);

public sealed record LeaveHistoryItemDto(
    Guid Id,
    string Title,
    string RecordType,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Days,
    string? Note);

public sealed record LeaveBancoHorasEntryDto(
    string Date,
    string Description,
    decimal Hours,
    string Type);

public sealed record LeaveBancoHorasDto(
    decimal BalanceHours,
    IReadOnlyList<LeaveBancoHorasEntryDto> Entries);

public sealed record LeaveTeamMemberDto(
    string Name,
    string Role,
    string AbsenceType,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record LeaveTeamCalendarDto(
    IReadOnlyList<LeaveTeamMemberDto> Members);

public sealed record CreateLeaveRequestDto(
    string ServiceId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Days,
    string? Notes);

public sealed record LeaveRequestResultDto(
    Guid RequestId,
    Guid RecordId,
    string Status,
    string Message);

public sealed record LeaveRequestItemDto(
    Guid Id,
    Guid? ServiceRequestId,
    string Title,
    string Status,
    string? RmSyncStatus,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Days,
    string? DataSource,
    DateTimeOffset CreatedAt);

public sealed record LeaveTimelineEventDto(
    string Label,
    string Status,
    DateTimeOffset OccurredAt,
    string? Detail);

public sealed record LeaveRequestDetailDto(
    Guid Id,
    Guid? ServiceRequestId,
    string Title,
    string Status,
    string? RmSyncStatus,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Days,
    string? Notes,
    string? DataSource,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LeaveTimelineEventDto> Timeline);

public sealed record LeaveManagementItemDto(
    Guid Id,
    Guid? ServiceRequestId,
    string EmployeeName,
    string? EmployeeId,
    string Email,
    string Title,
    string Status,
    string? RmSyncStatus,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Days,
    string? DataSource,
    DateTimeOffset CreatedAt);

public sealed record LeaveManagementDetailDto(
    Guid Id,
    Guid? ServiceRequestId,
    string EmployeeName,
    string? EmployeeId,
    string Email,
    string Title,
    string Status,
    string? RmSyncStatus,
    string? RmExternalId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? Days,
    string? Notes,
    string? DataSource,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LeaveTimelineEventDto> Timeline,
    string ApprovalNote);
