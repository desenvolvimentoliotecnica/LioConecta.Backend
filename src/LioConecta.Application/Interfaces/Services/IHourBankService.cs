using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IHourBankService
{
    Task<LeaveBancoHorasDto> GetMineAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HourBankTeamMemberDto>> GetTeamAsync(
        string? query = null,
        CancellationToken cancellationToken = default);

    Task<LeaveBancoHorasDto> GetForPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default);
}
