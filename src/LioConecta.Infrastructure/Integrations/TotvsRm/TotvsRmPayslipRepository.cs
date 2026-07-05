using Dapper;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public sealed class TotvsRmPayslipRepository(
    ITotvsRmConfigurationService configurationService,
    ILogger<TotvsRmPayslipRepository> logger) : ITotvsRmPayslipRepository
{
    public Task<IReadOnlyList<RmPayslipSummaryRecord>> GetPayslipSummariesAsync(
        string chapa,
        int maxEnvelopes,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                F.ANOCOMP AS AnoComp,
                F.MESCOMP AS MesComp,
                F.NROPERIODO AS NroPeriodo,
                SUM(CASE WHEN E.PROVDESCBASE = 'P' THEN F.VALOR ELSE 0 END) AS GrossAmount,
                SUM(CASE WHEN E.PROVDESCBASE = 'P' THEN F.VALOR ELSE 0 END)
                    - SUM(CASE WHEN E.PROVDESCBASE = 'D' THEN F.VALOR ELSE 0 END) AS NetAmount,
                SUM(CASE WHEN E.PROVDESCBASE = 'D' THEN F.VALOR ELSE 0 END) AS DeductionAmount,
                MAX(F.DTPAGTO) AS PaymentDate,
                MAX(CASE
                    WHEN E.PROVDESCBASE = 'P' AND (
                        LTRIM(RTRIM(F.CODEVENTO)) IN ('401', '0401')
                        OR LTRIM(RTRIM(E.DESCRICAO)) LIKE '%ADIANTAMENTO%'
                    ) THEN 1
                    ELSE 0
                END) AS HasAdvanceEvent,
                MAX(CASE
                    WHEN E.PROVDESCBASE = 'P' AND (
                        LTRIM(RTRIM(F.CODEVENTO)) NOT IN ('401', '0401')
                        AND LTRIM(RTRIM(E.DESCRICAO)) NOT LIKE '%ADIANTAMENTO%'
                    ) THEN 1
                    ELSE 0
                END) AS HasPayrollEvents
            FROM dbo.PFFINANC F WITH (NOLOCK)
            INNER JOIN dbo.PEVENTO E WITH (NOLOCK)
                ON E.CODCOLIGADA = F.CODCOLIGADA AND E.CODIGO = F.CODEVENTO
            WHERE F.CODCOLIGADA = @CodColigada
              AND F.CHAPA = @Chapa
            GROUP BY F.ANOCOMP, F.MESCOMP, F.NROPERIODO
            ORDER BY F.ANOCOMP DESC, F.MESCOMP DESC, MAX(F.DTPAGTO) DESC;
            """;

        return QueryAsync(
            TotvsRmConstants.PayrollFinanceTableName,
            chapa,
            async connection =>
            {
                var rows = await connection.QueryAsync<RmPayslipSummaryRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa
                });
                return (IReadOnlyList<RmPayslipSummaryRecord>)rows.Take(maxEnvelopes).ToList();
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<RmPayslipLineRecord>> GetPayslipLinesAsync(
        string chapa,
        int anoComp,
        int mesComp,
        int nroPeriodo,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                F.NROPERIODO AS NroPeriodo,
                LTRIM(RTRIM(F.CODEVENTO)) AS Code,
                COALESCE(LTRIM(RTRIM(E.DESCRICAO)), LTRIM(RTRIM(F.CODEVENTO))) AS Description,
                COALESCE(LTRIM(RTRIM(CAST(F.REF AS VARCHAR(32)))), '—') AS Reference,
                F.VALOR AS Amount,
                CASE WHEN E.PROVDESCBASE = 'D' THEN 1 ELSE 0 END AS IsDeduction,
                COALESCE(LTRIM(RTRIM(E.PROVDESCBASE)), '') AS ProvisionType
            FROM dbo.PFFINANC F WITH (NOLOCK)
            LEFT JOIN dbo.PEVENTO E WITH (NOLOCK)
                ON E.CODCOLIGADA = F.CODCOLIGADA AND E.CODIGO = F.CODEVENTO
            WHERE F.CODCOLIGADA = @CodColigada
              AND F.CHAPA = @Chapa
              AND F.ANOCOMP = @AnoComp
              AND F.MESCOMP = @MesComp
              AND F.NROPERIODO = @NroPeriodo
              AND F.VALOR <> 0
            ORDER BY E.PROVDESCBASE, F.CODEVENTO;
            """;

        return QueryAsync(
            TotvsRmConstants.PayrollFinanceTableName,
            chapa,
            async connection =>
            {
                var rows = await connection.QueryAsync<RmPayslipLineRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                    AnoComp = anoComp,
                    MesComp = mesComp,
                    NroPeriodo = nroPeriodo
                });
                return (IReadOnlyList<RmPayslipLineRecord>)rows.ToList();
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<RmPayslipLineRecord>> GetPayslipLinesForMonthAsync(
        string chapa,
        int anoComp,
        int mesComp,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                F.NROPERIODO AS NroPeriodo,
                LTRIM(RTRIM(F.CODEVENTO)) AS Code,
                COALESCE(LTRIM(RTRIM(E.DESCRICAO)), LTRIM(RTRIM(F.CODEVENTO))) AS Description,
                COALESCE(LTRIM(RTRIM(CAST(F.REF AS VARCHAR(32)))), '—') AS Reference,
                F.VALOR AS Amount,
                CASE WHEN E.PROVDESCBASE = 'D' THEN 1 ELSE 0 END AS IsDeduction,
                COALESCE(LTRIM(RTRIM(E.PROVDESCBASE)), '') AS ProvisionType
            FROM dbo.PFFINANC F WITH (NOLOCK)
            LEFT JOIN dbo.PEVENTO E WITH (NOLOCK)
                ON E.CODCOLIGADA = F.CODCOLIGADA AND E.CODIGO = F.CODEVENTO
            WHERE F.CODCOLIGADA = @CodColigada
              AND F.CHAPA = @Chapa
              AND F.ANOCOMP = @AnoComp
              AND F.MESCOMP = @MesComp
              AND F.VALOR <> 0
            ORDER BY F.NROPERIODO, E.PROVDESCBASE, F.CODEVENTO;
            """;

        return QueryAsync(
            TotvsRmConstants.PayrollFinanceTableName,
            chapa,
            async connection =>
            {
                var rows = await connection.QueryAsync<RmPayslipLineRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                    AnoComp = anoComp,
                    MesComp = mesComp
                });
                return (IReadOnlyList<RmPayslipLineRecord>)rows.ToList();
            },
            cancellationToken);
    }

    public async Task<RmPayslipPeriodRecord?> GetPayslipPeriodAsync(
        string chapa,
        int anoComp,
        int mesComp,
        int nroPeriodo,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                PER.NROPERIODO AS NroPeriodo,
                COALESCE(PER.BASEFGTS, 0) AS BaseFgts,
                COALESCE(PER.BASEIRRF, PER.BASEIR, 0) AS BaseIrrf,
                COALESCE(PER.BASEIRPLR, 0) AS BaseIrPlr,
                COALESCE(PER.BASEINSS, PER.SALARIO, PER.SALCONTRIB, 0) AS BaseInss,
                COALESCE(PER.VLRFGTS, PER.FGTSMES, PER.VALORFGTS, PER.FGTS, 0) AS FgtsAmount,
                COALESCE(PER.PENSAO, PER.PENSAOALIM, 0) AS PensionAlimony,
                COALESCE(PER.SALARIO, PER.SALBASE, 0) AS BaseSalary
            FROM dbo.PFPERFF PER WITH (NOLOCK)
            WHERE PER.CODCOLIGADA = @CodColigada
              AND PER.CHAPA = @Chapa
              AND PER.ANOCOMP = @AnoComp
              AND PER.MESCOMP = @MesComp
              AND PER.NROPERIODO = @NroPeriodo;
            """;

        try
        {
            return await QueryAsync(
                TotvsRmConstants.PayrollPeriodTableName,
                chapa,
                connection => connection.QueryFirstOrDefaultAsync<RmPayslipPeriodRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                    AnoComp = anoComp,
                    MesComp = mesComp,
                    NroPeriodo = nroPeriodo
                }),
                cancellationToken);
        }
        catch (TotvsRmIntegrationException)
        {
            return null;
        }
    }

    private async Task<T> QueryAsync<T>(
        string operationLabel,
        string chapa,
        Func<SqlConnection, Task<T>> queryFactory,
        CancellationToken cancellationToken)
    {
        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled)
        {
            throw new TotvsRmIntegrationDisabledException();
        }

        if (string.IsNullOrWhiteSpace(runtime.Password))
        {
            throw new TotvsRmIntegrationMisconfiguredException("Credenciais TOTVS RM incompletas.");
        }

        try
        {
            await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
            await connection.OpenAsync(cancellationToken);
            return await queryFactory(connection);
        }
        catch (TotvsRmIntegrationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Falha ao consultar TOTVS RM ({OperationLabel}) para CHAPA {Chapa}.",
                operationLabel,
                chapa);

            throw new TotvsRmIntegrationUnavailableException();
        }
    }
}
