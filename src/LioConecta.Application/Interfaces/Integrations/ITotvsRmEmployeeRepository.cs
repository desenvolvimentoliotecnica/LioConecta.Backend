using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ITotvsRmEmployeeRepository
{
    Task<RmEmployeeProfileRecord?> GetProfileByChapaAsync(
        string chapa,
        CancellationToken cancellationToken);
}
