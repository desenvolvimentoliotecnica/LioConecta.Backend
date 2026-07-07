namespace LioConecta.Application.DTOs;

public sealed record MenuSectionDto(
    string Key,
    string Label,
    string Value);

public sealed record MealMenuDto(
    string MealType,
    IReadOnlyList<MenuSectionDto> Sections);

public sealed record DailyMenuDto(
    DateOnly Date,
    string DayStatus,
    string? DayStatusLabel,
    IReadOnlyList<MealMenuDto> Meals,
    string? Notes,
    bool Published,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record WeeklyMenuDto(
    DateOnly WeekStart,
    IReadOnlyList<DailyMenuDto> Days);

public sealed record MenuSectionTemplateDto(
    string Key,
    string Label);

public sealed record MenuTemplatesDto(
    IReadOnlyList<MenuSectionTemplateDto> LunchSections,
    IReadOnlyList<string> MealTypes);

public sealed record MenuEditorBootstrapDto(
    bool CanEdit,
    MenuTemplatesDto Templates);

public sealed record SaveDailyMenuRequest(
    string? DayStatus,
    string? DayStatusLabel,
    IReadOnlyList<MealMenuDto> Meals,
    string? Notes,
    bool? Published);

public sealed record CopyMenuDayRequest(
    DateOnly SourceDate);

public sealed record CopyMenuWeekRequest(
    DateOnly SourceWeekStart,
    DateOnly? TargetWeekStart);

public sealed record SendFacilitiesMenuEmailRequest(
    DateOnly WeekStart,
    IReadOnlyList<string>? Recipients,
    bool IncludePdf);

public sealed record SendFacilitiesMenuEmailResponse(
    bool Success,
    string Message,
    int RecipientCount);

public sealed record FacilitiesMenuEditorPolicyDto(
    bool CanEdit);
