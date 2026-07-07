using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ICalendarConfigurationService
{
    Task<CalendarConnectionTestResponse> TestConnectionAsync(
        TestCalendarConnectionRequest request,
        CancellationToken cancellationToken = default);
}
