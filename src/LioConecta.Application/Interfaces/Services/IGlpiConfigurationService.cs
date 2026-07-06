using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IGlpiConfigurationService
{
    Task<GlpiConnectionTestResponse> TestConnectionAsync(
        TestGlpiConnectionRequest request,
        CancellationToken cancellationToken = default);
}
