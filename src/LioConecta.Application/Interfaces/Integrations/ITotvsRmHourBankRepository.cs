using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ITotvsRmHourBankRepository
{
    Task<RmHourBankBalanceRecord?> GetLatestBalanceAsync(
        string chapa,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RmHourBankBalanceRecord>> GetBalanceHistoryAsync(
        string chapa,
        int maxPeriods,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RmHourBankDayRecord>> GetDayMovementsAsync(
        string chapa,
        DateTime fromInclusive,
        DateTime toInclusive,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, RmHourBankBalanceRecord>> GetLatestBalancesByChapasAsync(
        IReadOnlyList<string> chapas,
        CancellationToken cancellationToken);
}
