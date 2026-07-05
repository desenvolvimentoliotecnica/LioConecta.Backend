using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IBenefitService
{
    Task<BenefitSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BenefitListItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<BenefitDetailDto?> GetDetailAsync(string benefitId, CancellationToken cancellationToken = default);

    Task<BenefitRequestResultDto> CreateRequestAsync(
        CreateBenefitRequestDto request,
        CancellationToken cancellationToken = default);
}
