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

public sealed record CreatePontoAdjustmentDayDto(
    DateOnly Date,
    string? OriginalClockIn,
    string? OriginalLunchOut,
    string? OriginalLunchIn,
    string? OriginalClockOut,
    string ClockIn,
    string LunchOut,
    string LunchIn,
    string ClockOut);

public sealed record CreatePontoAdjustmentDto(
    string Reason,
    IReadOnlyList<CreatePontoAdjustmentDayDto> Days);

public sealed record PontoAttachmentMetaDto(
    string FileName,
    string StorageFileName,
    string ContentType,
    long SizeBytes,
    string Url);

public sealed record PontoAttachmentInput(
    Stream Content,
    string FileName,
    string? ContentType,
    long SizeBytes);

public sealed record PontoAttachmentFileDto(
    byte[] Content,
    string ContentType,
    string FileName);

public sealed record PontoAdjustmentResultDto(
    Guid RequestId,
    Guid RecordId,
    string Status,
    string Message,
    string Protocol);

public sealed record PontoAdjustmentDayDetailDto(
    DateOnly Date,
    string OriginalClockIn,
    string OriginalLunchOut,
    string OriginalLunchIn,
    string OriginalClockOut,
    string ClockIn,
    string LunchOut,
    string LunchIn,
    string ClockOut);

public sealed record PontoAdjustmentTimelineEventDto(
    string Label,
    string Status,
    DateTimeOffset OccurredAt,
    string? Detail);

public sealed record PontoAdjustmentItemDto(
    Guid Id,
    Guid? ServiceRequestId,
    string Title,
    string Status,
    int DayCount,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record PontoAdjustmentDetailDto(
    Guid Id,
    Guid? ServiceRequestId,
    string Title,
    string Status,
    string Reason,
    int DayCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PontoAdjustmentDayDetailDto> Days,
    IReadOnlyList<PontoAdjustmentTimelineEventDto> Timeline,
    IReadOnlyList<PontoAttachmentMetaDto> Attachments);

public sealed record PontoAdjustmentManagementItemDto(
    Guid Id,
    Guid? ServiceRequestId,
    string EmployeeName,
    string? EmployeeId,
    string Email,
    string Title,
    string Status,
    int DayCount,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record PontoAdjustmentManagementDetailDto(
    Guid Id,
    Guid? ServiceRequestId,
    string EmployeeName,
    string? EmployeeId,
    string Email,
    string Title,
    string Status,
    string Reason,
    int DayCount,
    string? DataSource,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PontoAdjustmentDayDetailDto> Days,
    IReadOnlyList<PontoAdjustmentTimelineEventDto> Timeline,
    string InfoNote,
    IReadOnlyList<PontoAttachmentMetaDto> Attachments);
