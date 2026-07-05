using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ITotvsRmTimesheetRepository
{
    Task<IReadOnlyList<Models.RmPunchRecord>> GetPunchesAsync(
        string chapa,
        DateTime dataDe,
        DateTime dataAte,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Models.RmProcessedDayRecord>> GetProcessedDaysAsync(
        string chapa,
        DateTime dataDe,
        DateTime dataAte,
        CancellationToken cancellationToken);
}
