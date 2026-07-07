using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IPayslipRepository
{
    Task<Payslip?> GetByCompetenceAsync(
        Guid personId,
        int year,
        int month,
        string? paymentType = null,
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

    Task UpsertAsync(Payslip payslip, CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetMaxSyncedAtUtcAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<int> DeleteWithoutSourceAsync(
        Guid personId,
        string requiredSource,
        CancellationToken cancellationToken = default);

    Task<int> DeleteBeforeCompetenceAsync(
        Guid personId,
        int fromYear,
        int fromMonth,
        CancellationToken cancellationToken = default);

    Task UpsertIncomeStatementAsync(
        IncomeStatement statement,
        CancellationToken cancellationToken = default);

    Task<int> DeleteIncomeStatementsWithoutSourceAsync(
        Guid personId,
        string requiredSource,
        CancellationToken cancellationToken = default);

    Task<int> DeleteIncomeStatementsBeforeYearAsync(
        Guid personId,
        int fromYear,
        CancellationToken cancellationToken = default);
}
