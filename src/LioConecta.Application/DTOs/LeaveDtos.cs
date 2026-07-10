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
    IReadOnlyList<LeaveBancoHorasEntryDto> Entries,
    string? PeriodLabel = null,
    string? DataSource = null,
    string? AvailabilityStatus = null,
    string? UserMessage = null);

public sealed record HourBankTeamMemberDto(
    Guid PersonId,
    string Name,
    string? Role,
    string? EmployeeId,
    decimal BalanceHours,
    string? PeriodLabel);

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

public sealed record LeaveAttachmentMetaDto(
    string FileName,
    string StorageFileName,
    string ContentType,
    long SizeBytes,
    string Url);

public sealed record LeaveAttachmentInput(
    Stream Content,
    string FileName,
    string? ContentType,
    long SizeBytes);

public sealed record LeaveRequestResultDto(
    Guid RequestId,
    Guid RecordId,
    string Status,
    string Message,
    string Protocol);

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
    IReadOnlyList<LeaveTimelineEventDto> Timeline,
    IReadOnlyList<LeaveAttachmentMetaDto> Attachments);

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
    string ApprovalNote,
    IReadOnlyList<LeaveAttachmentMetaDto> Attachments);

public sealed record LeaveAttachmentFileDto(
    byte[] Content,
    string ContentType,
    string FileName);
