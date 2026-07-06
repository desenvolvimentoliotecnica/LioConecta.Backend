using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IGraphConfigurationService
{
    Task<GraphConnectionTestResponse> TestConnectionAsync(
        TestGraphConnectionRequest request,
        CancellationToken cancellationToken = default);
}
