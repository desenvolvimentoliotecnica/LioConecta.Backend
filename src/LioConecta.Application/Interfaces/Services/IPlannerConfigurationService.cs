using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPlannerConfigurationService
{
    Task<PlannerConnectionTestResponse> TestConnectionAsync(
        TestPlannerConnectionRequest request,
        CancellationToken cancellationToken = default);
}
