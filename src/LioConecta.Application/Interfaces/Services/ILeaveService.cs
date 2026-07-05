using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ILeaveService
{
    Task<LeaveSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<LeaveServiceDto> GetServices();

    Task<LeaveBalanceDto> GetBalanceAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveHistoryItemDto>> GetHistoryAsync(int limit, CancellationToken cancellationToken = default);

    Task<LeaveBancoHorasDto> GetBancoHorasAsync(CancellationToken cancellationToken = default);

    Task<LeaveTeamCalendarDto> GetTeamCalendarAsync(CancellationToken cancellationToken = default);

    Task<LeaveRequestResultDto> CreateRequestAsync(
        CreateLeaveRequestDto request,
        CancellationToken cancellationToken = default);
}
