using Dapper;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Services;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

public sealed class TotvsRmLeaveRepository(
    ITotvsRmConfigurationService configurationService,
    ILogger<TotvsRmLeaveRepository> logger) : ITotvsRmLeaveRepository
{
    public async Task<RmLeaveBalanceData?> GetLeaveDataByChapaAsync(
        string chapa,
        CancellationToken cancellationToken = default)
    {
        var periods = await QueryPeriodsAsync(chapa, cancellationToken);
        if (periods is null)
        {
            return null;
        }

        var requests = await QueryVacationRequestsAsync(chapa, cancellationToken) ?? [];

        var availableDays = periods.Sum(period => Math.Max(0, period.SaldoDias));
        var acquiredDays = periods.Sum(period => Math.Max(0, period.DiasAdquiridos));
        var usedDays = periods.Sum(period => Math.Max(0, period.DiasUsados));
        var expiredDays = periods
            .Where(period => period.DataVencimento is not null && period.DataVencimento < DateOnly.FromDateTime(DateTime.UtcNow))
            .Sum(period => Math.Max(0, period.SaldoDias));

        var scheduled = requests
            .Where(request => LeaveStatusNormalizer.FromRm(request.RmStatus, request.StartDate, request.EndDate) is "pending" or "approved")
            .ToList();

        var scheduledDays = scheduled.Sum(request => request.Days ?? 0);
        var nextScheduled = scheduled
            .Where(request => request.StartDate is not null)
            .OrderBy(request => request.StartDate)
            .FirstOrDefault();

        return new RmLeaveBalanceData(
            availableDays,
            acquiredDays,
            scheduledDays,
            expiredDays,
            nextScheduled?.StartDate,
            nextScheduled?.EndDate,
            periods,
            requests);
    }

    private async Task<IReadOnlyList<RmLeavePeriodRecord>?> QueryPeriodsAsync(
        string chapa,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CAST(F.INICIOPERAQUIS AS date) AS InicioPeriodo,
                CAST(F.FIMPERAQUIS AS date) AS FimPeriodo,
                CAST(ISNULL(F.SALDOPER, 0) AS int) AS SaldoDias,
                CAST(ISNULL(F.NRODIAS, 0) AS int) AS DiasAdquiridos,
                CAST(ISNULL(F.NRODIAS, 0) - ISNULL(F.SALDOPER, 0) AS int) AS DiasUsados,
                CAST(F.DTVENCFERIAS AS date) AS DataVencimento
            FROM dbo.PFUFERIAS F WITH (NOLOCK)
            WHERE F.CODCOLIGADA = @CodColigada
              AND LTRIM(RTRIM(F.CHAPA)) = @Chapa
            ORDER BY F.FIMPERAQUIS DESC;
            """;

        try
        {
            var rows = await QueryListAsync(
                "PFUFERIAS periods",
                chapa,
                cancellationToken,
                connection => connection.QueryAsync<RmLeavePeriodRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                }));

            return rows?.ToList() ?? [];
        }
        catch (SqlException exception) when (exception.Number is 208 or 3701)
        {
            logger.LogWarning("Tabela PFUFERIAS indisponível no RM para CHAPA {Chapa}.", chapa);
            return [];
        }
    }

    private async Task<IReadOnlyList<RmVacationRequestRecord>?> QueryVacationRequestsAsync(
        string chapa,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                CONCAT('rm:', LTRIM(RTRIM(P.CHAPA)), ':', CONVERT(varchar(8), P.DTINIGOZO, 112), ':', CONVERT(varchar(8), P.DTFIMGOZO, 112)) AS ExternalId,
                CAST(P.DTINIGOZO AS date) AS StartDate,
                CAST(P.DTFIMGOZO AS date) AS EndDate,
                CAST(P.NRODIAS AS int) AS Days,
                LTRIM(RTRIM(P.SITUACAOFERIAS)) AS RmStatus,
                CONCAT('Férias — ', FORMAT(P.DTINIGOZO, 'MMM/yyyy', 'pt-BR')) AS Title
            FROM dbo.PFUFERIASPER P WITH (NOLOCK)
            WHERE P.CODCOLIGADA = @CodColigada
              AND LTRIM(RTRIM(P.CHAPA)) = @Chapa
              AND P.DTINIGOZO IS NOT NULL
            ORDER BY P.DTINIGOZO DESC;
            """;

        try
        {
            var rows = await QueryListAsync(
                "PFUFERIASPER requests",
                chapa,
                cancellationToken,
                connection => connection.QueryAsync<RmVacationRequestRecord>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                }));

            return rows?.ToList() ?? [];
        }
        catch (SqlException exception) when (exception.Number is 208 or 3701)
        {
            logger.LogWarning("Tabela PFUFERIASPER indisponível no RM para CHAPA {Chapa}.", chapa);
            return [];
        }
    }

    private async Task<IEnumerable<T>?> QueryListAsync<T>(
        string operationLabel,
        string chapa,
        CancellationToken cancellationToken,
        Func<SqlConnection, Task<IEnumerable<T>>> queryFactory)
    {
        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled || string.IsNullOrWhiteSpace(runtime.Password))
        {
            return null;
        }

        try
        {
            await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
            await connection.OpenAsync(cancellationToken);
            return await queryFactory(connection);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Falha ao consultar TOTVS RM ({OperationLabel}) para CHAPA {Chapa}.",
                operationLabel,
                chapa);

            return null;
        }
    }
}
