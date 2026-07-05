using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ITotvsRmConfigurationService
{
    Task<TotvsRmConfigurationDto> GetAsync(CancellationToken cancellationToken);

    Task<TotvsRmConfigurationDto> SaveAsync(
        UpsertTotvsRmConfigurationRequest request,
        CancellationToken cancellationToken);

    Task<TotvsRmRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken);

    Task<TotvsRmConnectionTestResponse> TestConnectionAsync(
        UpsertTotvsRmConfigurationRequest request,
        CancellationToken cancellationToken);

    Task EnsureDefaultConfigurationAsync(CancellationToken cancellationToken);
}
