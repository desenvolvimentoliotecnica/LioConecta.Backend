using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPersonService
{
    Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default);

    Task<PersonProfileDto?> GetProfileAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonSummaryDto>> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default);

    Task<OrgChartDto> GetOrgChartAsync(CancellationToken cancellationToken = default);
}
