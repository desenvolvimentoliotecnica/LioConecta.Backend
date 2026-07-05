using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IUserPreferenceService
{
    Task<UserPreferencesDto> GetAsync(CancellationToken cancellationToken = default);

    Task<UserPreferencesDto> UpdateAsync(UpdatePreferencesRequest request, CancellationToken cancellationToken = default);
}
