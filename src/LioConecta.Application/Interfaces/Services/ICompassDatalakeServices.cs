using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ICompassScenarioQueryService
{
    Task<CompassScenariosDto> GetScenariosAsync(
        CompassScenariosQuery query,
        CancellationToken cancellationToken = default);

    Task<CompassScenarioRowsPageDto> GetScenarioRowsAsync(
        string scenarioId,
        CompassScenarioRowsQuery query,
        CancellationToken cancellationToken = default);
}
