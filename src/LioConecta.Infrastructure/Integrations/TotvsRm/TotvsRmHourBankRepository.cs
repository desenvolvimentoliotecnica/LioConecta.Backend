using Dapper;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public sealed class TotvsRmHourBankRepository(
    ITotvsRmConfigurationService configurationService,
    ILogger<TotvsRmHourBankRepository> logger) : ITotvsRmHourBankRepository
{
    public async Task<RmHourBankBalanceRecord?> GetLatestBalanceAsync(
        string chapa,
        CancellationToken cancellationToken)
    {
        var rows = await GetBalanceHistoryAsync(chapa, 1, cancellationToken);
        return rows.FirstOrDefault();
    }

    public Task<IReadOnlyList<RmHourBankBalanceRecord>> GetBalanceHistoryAsync(
        string chapa,
        int maxPeriods,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (@MaxPeriods)
                S.CHAPA                          AS Chapa,
                S.INICIOPER                      AS PeriodStart,
                S.FIMPER                         AS PeriodEnd,
                COALESCE(S.EXTRAANT, 0)          AS ExtraPreviousMinutes,
                COALESCE(S.EXTRAATU, 0)          AS ExtraCurrentMinutes,
                COALESCE(S.ATRASOANT, 0)         AS DelayPreviousMinutes,
                COALESCE(S.ATRASOATU, 0)         AS DelayCurrentMinutes,
                COALESCE(S.FALTAANT, 0)          AS AbsencePreviousMinutes,
                COALESCE(S.FALTAATU, 0)          AS AbsenceCurrentMinutes
            FROM dbo.ASALDOBANCOHOR S WITH (NOLOCK)
            WHERE S.CODCOLIGADA = @CodColigada
              AND S.CHAPA = @Chapa
            ORDER BY S.FIMPER DESC;
            """;

        return QueryAsync<RmHourBankBalanceRecord>(
            "ASALDOBANCOHOR",
            chapa,
            async connection =>
            {
                var rows = await connection.QueryAsync<RmHourBankBalanceRecord>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            CodColigada = TotvsRmConstants.CodColigada,
                            Chapa = chapa,
                            MaxPeriods = Math.Clamp(maxPeriods, 1, 36),
                        },
                        cancellationToken: cancellationToken));
                return rows.ToList();
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<RmHourBankDayRecord>> GetDayMovementsAsync(
        string chapa,
        DateTime fromInclusive,
        DateTime toInclusive,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CAST(B.DATA AS DATE) AS Date,
                (
                    COALESCE(B.EXTRAFAIXA1, 0) + COALESCE(B.EXTRAFAIXA2, 0) + COALESCE(B.EXTRAFAIXA3, 0)
                    + COALESCE(B.EXTRAFAIXA4, 0) + COALESCE(B.EXTRAFAIXA5, 0)
                    + COALESCE(B.EXTRADESC1, 0) + COALESCE(B.EXTRADESC2, 0)
                    + COALESCE(B.EXTRAFER1, 0) + COALESCE(B.EXTRAFER2, 0)
                    + COALESCE(B.EXTRACOMP1, 0) + COALESCE(B.EXTRACOMP2, 0)
                ) AS ExtraMinutes,
                COALESCE(B.ATRASO, 0) AS DelayMinutes,
                COALESCE(B.FALTA, 0) AS AbsenceMinutes
            FROM dbo.ABANCOHORFUN B WITH (NOLOCK)
            WHERE B.CODCOLIGADA = @CodColigada
              AND B.CHAPA = @Chapa
              AND CAST(B.DATA AS DATE) BETWEEN @FromDate AND @ToDate
              AND (
                    COALESCE(B.EXTRAFAIXA1, 0) + COALESCE(B.EXTRAFAIXA2, 0) + COALESCE(B.EXTRAFAIXA3, 0)
                    + COALESCE(B.EXTRAFAIXA4, 0) + COALESCE(B.EXTRAFAIXA5, 0)
                    + COALESCE(B.EXTRADESC1, 0) + COALESCE(B.EXTRADESC2, 0)
                    + COALESCE(B.EXTRAFER1, 0) + COALESCE(B.EXTRAFER2, 0)
                    + COALESCE(B.EXTRACOMP1, 0) + COALESCE(B.EXTRACOMP2, 0)
                    + COALESCE(B.ATRASO, 0) + COALESCE(B.FALTA, 0)
                  ) <> 0
            ORDER BY B.DATA DESC;
            """;

        return QueryAsync<RmHourBankDayRecord>(
            "ABANCOHORFUN",
            chapa,
            async connection =>
            {
                var rows = await connection.QueryAsync<RmHourBankDayRecord>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            CodColigada = TotvsRmConstants.CodColigada,
                            Chapa = chapa,
                            FromDate = fromInclusive.Date,
                            ToDate = toInclusive.Date,
                        },
                        cancellationToken: cancellationToken));
                return rows.ToList();
            },
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, RmHourBankBalanceRecord>> GetLatestBalancesByChapasAsync(
        IReadOnlyList<string> chapas,
        CancellationToken cancellationToken)
    {
        if (chapas.Count == 0)
        {
            return new Dictionary<string, RmHourBankBalanceRecord>(StringComparer.OrdinalIgnoreCase);
        }

        const string sql = """
            WITH Ranked AS (
                SELECT
                    S.CHAPA                          AS Chapa,
                    S.INICIOPER                      AS PeriodStart,
                    S.FIMPER                         AS PeriodEnd,
                    COALESCE(S.EXTRAANT, 0)          AS ExtraPreviousMinutes,
                    COALESCE(S.EXTRAATU, 0)          AS ExtraCurrentMinutes,
                    COALESCE(S.ATRASOANT, 0)         AS DelayPreviousMinutes,
                    COALESCE(S.ATRASOATU, 0)         AS DelayCurrentMinutes,
                    COALESCE(S.FALTAANT, 0)          AS AbsencePreviousMinutes,
                    COALESCE(S.FALTAATU, 0)          AS AbsenceCurrentMinutes,
                    ROW_NUMBER() OVER (PARTITION BY S.CHAPA ORDER BY S.FIMPER DESC) AS Rn
                FROM dbo.ASALDOBANCOHOR S WITH (NOLOCK)
                WHERE S.CODCOLIGADA = @CodColigada
                  AND S.CHAPA IN @Chapas
            )
            SELECT Chapa, PeriodStart, PeriodEnd,
                   ExtraPreviousMinutes, ExtraCurrentMinutes,
                   DelayPreviousMinutes, DelayCurrentMinutes,
                   AbsencePreviousMinutes, AbsenceCurrentMinutes
            FROM Ranked
            WHERE Rn = 1;
            """;

        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        EnsureRuntime(runtime);

        try
        {
            await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
            await connection.OpenAsync(cancellationToken);
            var rows = await connection.QueryAsync<RmHourBankBalanceRecord>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        CodColigada = TotvsRmConstants.CodColigada,
                        Chapas = chapas.ToArray(),
                    },
                    cancellationToken: cancellationToken));

            return rows.ToDictionary(r => r.Chapa, StringComparer.OrdinalIgnoreCase);
        }
        catch (TotvsRmIntegrationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao consultar saldos de banco de horas em lote no TOTVS RM.");
            throw new TotvsRmIntegrationUnavailableException();
        }
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(
        string operationLabel,
        string chapa,
        Func<SqlConnection, Task<IReadOnlyList<T>>> queryFactory,
        CancellationToken cancellationToken)
    {
        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        EnsureRuntime(runtime);

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
                "Falha ao consultar TOTVS RM ({OperationLabel}) banco de horas para CHAPA {Chapa}.",
                operationLabel,
                chapa);
            throw new TotvsRmIntegrationUnavailableException();
        }
    }

    private static void EnsureRuntime(TotvsRmRuntimeConfiguration runtime)
    {
        if (!runtime.IsEnabled)
        {
            throw new TotvsRmIntegrationDisabledException();
        }

        if (string.IsNullOrWhiteSpace(runtime.Password))
        {
            throw new TotvsRmIntegrationMisconfiguredException("Credenciais TOTVS RM incompletas.");
        }
    }
}
