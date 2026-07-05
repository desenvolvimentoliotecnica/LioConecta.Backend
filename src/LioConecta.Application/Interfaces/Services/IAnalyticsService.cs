using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IAnalyticsService
{
    Task<AnalyticsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
