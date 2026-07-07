using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IFacilitiesMenuService
{
    Task<MenuEditorBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);

    Task<FacilitiesMenuEditorPolicyDto> GetEditorPolicyAsync(CancellationToken cancellationToken = default);

    Task<DailyMenuDto?> GetPublishedDailyMenuAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<WeeklyMenuDto> GetWeeklyMenuAsync(DateOnly weekStart, CancellationToken cancellationToken = default);

    Task<DailyMenuDto> SaveDailyMenuAsync(
        DateOnly date,
        SaveDailyMenuRequest request,
        CancellationToken cancellationToken = default);

    Task<DailyMenuDto> CopyDailyMenuAsync(
        DateOnly targetDate,
        DateOnly sourceDate,
        CancellationToken cancellationToken = default);

    Task<WeeklyMenuDto> CopyWeeklyMenuAsync(
        DateOnly targetWeekStart,
        DateOnly sourceWeekStart,
        CancellationToken cancellationToken = default);

    Task DeleteDailyMenuAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<SendFacilitiesMenuEmailResponse> SendWeeklyEmailAsync(
        SendFacilitiesMenuEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> GetWeeklyMenuPdfAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
}
