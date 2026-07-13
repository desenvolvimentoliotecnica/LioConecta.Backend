using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ICompassService
{
    Task<CompassBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);

    Task<CompassMetaDto> GetMetaAsync(CancellationToken cancellationToken = default);

    Task<CompassDashboardDto> GetDashboardAsync(CompassYtdQuery query, CancellationToken cancellationToken = default);

    Task<CompassYtdPageDto> GetYtdPageAsync(CompassYtdQuery query, CancellationToken cancellationToken = default);

    Task<CompassAggregatesDto> GetAggregatesAsync(CompassAggregatesQuery query, CancellationToken cancellationToken = default);

    Task<CompassScenariosDto> GetScenariosAsync(CompassScenariosQuery query, CancellationToken cancellationToken = default);

    Task<CompassScenarioRowsPageDto> GetScenarioRowsAsync(
        string scenarioId,
        CompassScenarioRowsQuery query,
        CancellationToken cancellationToken = default);
}
