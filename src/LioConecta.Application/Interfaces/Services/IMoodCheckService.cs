using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IMoodCheckService
{
    Task<MoodTodayDto> GetTodayAsync(CancellationToken cancellationToken = default);

    Task<RegisterMoodResultDto> RegisterAsync(
        RegisterMoodRequest request,
        CancellationToken cancellationToken = default);

    Task<MoodMetricsDto> GetMetricsAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
