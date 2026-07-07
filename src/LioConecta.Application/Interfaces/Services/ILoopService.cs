using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ILoopService
{
    Task<LoopBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);
}
