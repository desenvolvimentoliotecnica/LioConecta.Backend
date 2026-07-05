using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IPayslipRepository
{
    Task<Payslip?> GetByCompetenceAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payslip>> ListAsync(
        Guid personId,
        int? year,
        int? month,
        int limit,
        CancellationToken cancellationToken = default);

    Task<Payslip?> GetLatestAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IncomeStatement?> GetIncomeStatementAsync(
        Guid personId,
        int year,
        CancellationToken cancellationToken = default);
}
