using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IActivityService
{
    Task<IReadOnlyList<ActivityDto>> GetRecentAsync(int limit = 20, CancellationToken cancellationToken = default);
}
