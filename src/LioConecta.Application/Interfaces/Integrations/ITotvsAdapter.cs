using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ITotvsAdapter
{
    Task<IReadOnlyList<TotvsEmployee>> SyncEmployeesAsync(CancellationToken cancellationToken = default);

    Task<byte[]> GetPayslipAsync(Guid personId, int year, int month, CancellationToken cancellationToken = default);

    Task<decimal> GetVacationBalanceAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<string> SubmitVacationRequestAsync(Guid personId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, object?>> GetBenefitsAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetTimeClockAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
