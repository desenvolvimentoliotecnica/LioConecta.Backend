using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task RecordLogoutAsync(CancellationToken cancellationToken = default);
}
