using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ITotvsRmLeaveRepository
{
    Task<RmLeaveBalanceData?> GetLeaveDataByChapaAsync(
        string chapa,
        CancellationToken cancellationToken = default);
}
