using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface ITotvsRmPayslipRepository
{
    Task<IReadOnlyList<RmPayslipSummaryRecord>> GetPayslipSummariesAsync(
        string chapa,
        int maxEnvelopes,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RmPayslipLineRecord>> GetPayslipLinesAsync(
        string chapa,
        int anoComp,
        int mesComp,
        int nroPeriodo,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RmPayslipLineRecord>> GetPayslipLinesForMonthAsync(
        string chapa,
        int anoComp,
        int mesComp,
        CancellationToken cancellationToken);

    Task<RmPayslipPeriodRecord?> GetPayslipPeriodAsync(
        string chapa,
        int anoComp,
        int mesComp,
        int nroPeriodo,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RmIncomeStatementLineRecord>> GetIncomeStatementLinesAsync(
        string chapa,
        int anoComp,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RmFgtsDepositRecord>> GetFgtsDepositsAsync(
        string chapa,
        int maxMonths,
        CancellationToken cancellationToken);
}
