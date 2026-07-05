using Dapper;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public sealed class TotvsRmTimesheetRepository(
    ITotvsRmConfigurationService configurationService,
    ILogger<TotvsRmTimesheetRepository> logger) : ITotvsRmTimesheetRepository
{
    public Task<IReadOnlyList<RmPunchRecord>> GetPunchesAsync(
        string chapa,
        DateTime dataDe,
        DateTime dataAte,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CAST(B.DATA AS DATE)            AS DataPonto,
                B.BATIDA                        AS BatidaMinutos,
                B.NATUREZA                      AS Natureza,
                NAT.DESCRICAO                   AS DescricaoNatureza,
                B.STATUS                        AS Status,
                CAST(B.CODIGOTERMCOL AS VARCHAR(32)) AS CodigoRelogio
            FROM dbo.ABATFUN B WITH (NOLOCK)
            LEFT JOIN dbo.ANATUBAT NAT WITH (NOLOCK)
                ON NAT.CODINTERNO = B.NATUREZA
            WHERE B.CODCOLIGADA = @CodColigada
              AND B.CHAPA       = @Chapa
              AND CAST(B.DATA AS DATE) BETWEEN @DataDe AND @DataAte
            ORDER BY B.DATA, B.BATIDA;
            """;

        return QueryAsync<RmPunchRecord>(
            "ABATFUN",
            chapa,
            dataDe,
            dataAte,
            async connection =>
            {
                var rows = await connection.QueryAsync<RmPunchRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                    DataDe = dataDe.Date,
                    DataAte = dataAte.Date
                });
                return rows.ToList();
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<RmProcessedDayRecord>> GetProcessedDaysAsync(
        string chapa,
        DateTime dataDe,
        DateTime dataAte,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CAST(H.DATA AS DATE) AS DataPonto,
                H.HTRAB              AS WorkedMinutes,
                COALESCE(H.BASE, H.TEMPOREF) AS ExpectedMinutes,
                CASE
                    WHEN COALESCE(H.BASE, H.TEMPOREF) IS NOT NULL
                        THEN COALESCE(H.HTRAB, 0)
                           + COALESCE(H.ABONO, 0)
                           + COALESCE(H.COMPENSADO, 0)
                           - COALESCE(H.BASE, H.TEMPOREF, 0)
                    ELSE NULL
                END AS BalanceMinutes,
                COALESCE(H.ATRASOCALC, H.ATRASO) AS DelayMinutes,
                COALESCE(H.FALTACALC, H.FALTA) AS AbsenceMinutes,
                CASE
                    WHEN COALESCE(H.ABONO, 0) > 0 THEN 'B'
                    WHEN COALESCE(H.EXTRAAUTORIZADO, 0) > 0 THEN 'E'
                    WHEN COALESCE(H.FALTACALC, H.FALTA, 0) > 0 THEN 'F'
                    WHEN COALESCE(H.ATRASOCALC, H.ATRASO, 0) > 0 THEN 'A'
                    ELSE 'D'
                END AS StatusCode
            FROM dbo.AAFHTFUN H WITH (NOLOCK)
            WHERE H.CODCOLIGADA = @CodColigada
              AND H.CHAPA       = @Chapa
              AND CAST(H.DATA AS DATE) BETWEEN @DataDe AND @DataAte
            ORDER BY H.DATA;
            """;

        return QueryAsync<RmProcessedDayRecord>(
            TotvsRmConstants.ProcessedDayTableName,
            chapa,
            dataDe,
            dataAte,
            async connection =>
            {
                var rows = await connection.QueryAsync<RmProcessedDayRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                    DataDe = dataDe.Date,
                    DataAte = dataAte.Date
                });
                return rows.ToList();
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(
        string operationLabel,
        string chapa,
        DateTime dataDe,
        DateTime dataAte,
        Func<SqlConnection, Task<IReadOnlyList<T>>> queryFactory,
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
                "Falha ao consultar TOTVS RM ({OperationLabel}) para CHAPA {Chapa} entre {DataDe:yyyy-MM-dd} e {DataAte:yyyy-MM-dd}.",
                operationLabel,
                chapa,
                dataDe,
                dataAte);

            throw new TotvsRmIntegrationUnavailableException();
        }
    }
}
