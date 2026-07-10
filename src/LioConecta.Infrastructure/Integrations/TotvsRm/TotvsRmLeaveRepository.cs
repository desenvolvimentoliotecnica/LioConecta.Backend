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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var availableDays = 0;
        var acquiringDays = 0;
        var expiredDays = 0;
        DateOnly? nextLiberationAt = null;

        foreach (var period in periods)
        {
            var saldo = Math.Max(0, period.SaldoDias);
            if (saldo <= 0)
            {
                continue;
            }

            var status = LeavePeriodClassifier.Classify(period.FimPeriodo, period.DataVencimento, today);
            switch (status)
            {
                case LeavePeriodClassifier.StatusEmAquisicao:
                    acquiringDays += saldo;
                    if (period.FimPeriodo is not null &&
                        (nextLiberationAt is null || period.FimPeriodo < nextLiberationAt))
                    {
                        nextLiberationAt = period.FimPeriodo;
                    }

                    break;
                case LeavePeriodClassifier.StatusVencido:
                    expiredDays += saldo;
                    break;
                default:
                    availableDays += saldo;
                    break;
            }
        }

        var acquiredDays = periods.Sum(period => Math.Max(0, period.DiasAdquiridos));

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
            acquiringDays,
            acquiredDays,
            scheduledDays,
            expiredDays,
            nextLiberationAt,
            nextScheduled?.StartDate,
            nextScheduled?.EndDate,
            periods,
            requests);
    }

    private async Task<IReadOnlyList<RmLeavePeriodRecord>?> QueryPeriodsAsync(
        string chapa,
        CancellationToken cancellationToken)
    {
        // Schema real CORPORERM (docs/spike-ferias-rm.md): SALDO (não SALDOPER);
        // sem NRODIAS/DTVENCFERIAS — vencimento concessivo = FIMPERAQUIS + 1 ano.
        // DiasAdquiridos ≈ SALDO (sem coluna de adquiridos); DiasUsados fica 0 na leitura.
        const string sql = """
            SELECT
                CAST(F.INICIOPERAQUIS AS date) AS InicioPeriodo,
                CAST(F.FIMPERAQUIS AS date) AS FimPeriodo,
                CAST(ISNULL(F.SALDO, 0) AS int) AS SaldoDias,
                CAST(ISNULL(F.SALDO, 0) AS int) AS DiasAdquiridos,
                0 AS DiasUsados,
                CAST(DATEADD(year, 1, F.FIMPERAQUIS) AS date) AS DataVencimento
            FROM dbo.PFUFERIAS F WITH (NOLOCK)
            WHERE F.CODCOLIGADA = @CodColigada
              AND LTRIM(RTRIM(F.CHAPA)) = @Chapa
              AND ISNULL(F.PERIODOPERDIDO, 0) = 0
              AND ISNULL(F.SALDO, 0) > 0
            ORDER BY F.FIMPERAQUIS DESC;
            """;

        try
        {
            var rows = await QueryListAsync(
                "PFUFERIAS periods",
                chapa,
                cancellationToken,
                connection => connection.QueryAsync<RmLeavePeriodRow>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                }));

            return rows?
                .Select(row => new RmLeavePeriodRecord(
                    ToDateOnly(row.InicioPeriodo),
                    ToDateOnly(row.FimPeriodo),
                    row.SaldoDias,
                    row.DiasAdquiridos,
                    row.DiasUsados,
                    ToDateOnly(row.DataVencimento)))
                .ToList() ?? [];
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
        // Schema real (ver docs/spike-writeback-sql-rm.md): PFUFERIASPER usa
        // DATAINICIO/DATAFIM/NRODIASFERIAS — não DTINIGOZO/DTFIMGOZO/NRODIAS.
        const string sql = """
            SELECT
                CONCAT('rm:', LTRIM(RTRIM(P.CHAPA)), ':', CONVERT(varchar(8), P.DATAINICIO, 112), ':', CONVERT(varchar(8), P.DATAFIM, 112)) AS ExternalId,
                CAST(P.DATAINICIO AS date) AS StartDate,
                CAST(P.DATAFIM AS date) AS EndDate,
                CAST(P.NRODIASFERIAS AS int) AS Days,
                LTRIM(RTRIM(P.SITUACAOFERIAS)) AS RmStatus,
                CONCAT('Férias — ', FORMAT(P.DATAINICIO, 'MMM/yyyy', 'pt-BR')) AS Title
            FROM dbo.PFUFERIASPER P WITH (NOLOCK)
            WHERE P.CODCOLIGADA = @CodColigada
              AND LTRIM(RTRIM(P.CHAPA)) = @Chapa
              AND P.DATAINICIO IS NOT NULL
            ORDER BY P.DATAINICIO DESC;
            """;

        try
        {
            var rows = await QueryListAsync(
                "PFUFERIASPER requests",
                chapa,
                cancellationToken,
                connection => connection.QueryAsync<RmVacationRequestRow>(sql, new
                {
                    CodColigada = TotvsRmConstants.CodColigada,
                    Chapa = chapa,
                }));

            return rows?
                .Select(row => new RmVacationRequestRecord(
                    row.ExternalId,
                    ToDateOnly(row.StartDate),
                    ToDateOnly(row.EndDate),
                    row.Days,
                    row.RmStatus,
                    row.Title))
                .ToList() ?? [];
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

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value);

    // Dapper materializa datetime SQL como DateTime — records com DateOnly falham.
    private sealed class RmLeavePeriodRow
    {
        public DateTime? InicioPeriodo { get; init; }
        public DateTime? FimPeriodo { get; init; }
        public int SaldoDias { get; init; }
        public int DiasAdquiridos { get; init; }
        public int DiasUsados { get; init; }
        public DateTime? DataVencimento { get; init; }
    }

    private sealed class RmVacationRequestRow
    {
        public string ExternalId { get; init; } = string.Empty;
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public int? Days { get; init; }
        public string? RmStatus { get; init; }
        public string? Title { get; init; }
    }
}
