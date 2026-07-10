using System.Text.Json;
using Dapper;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

/// <summary>
/// Write-back SQL direto no TOTVS RM (Onda 1B): INSERT em PFUFERIASPER + UPDATE do
/// saldo em PFUFERIAS, com journal reverso para rollback. Ver docs/spike-writeback-sql-rm.md.
/// NOTA: a credencial padrão do portal (rm_readonly_*) não possui permissão de escrita —
/// configure em Configurações do Backend uma connection string TOTVS RM com INSERT/UPDATE
/// antes de habilitar modos apply*/apply em homologação.
/// </summary>
public sealed class TotvsRmSqlLeaveWriteBack(
    ITotvsRmConfigurationService configurationService,
    IRmWriteBackJournalService journalService,
    IAppSettingsProvider settings,
    ILogger<TotvsRmSqlLeaveWriteBack> logger) : ILeaveRmWriteBack
{
    public async Task<LeaveRmWriteBackResult> SubmitVacationRequestAsync(
        LeaveRmWriteBackCommand command,
        CancellationToken cancellationToken = default)
    {
        var mode = RmWriteBackModes.ResolveLeaveMode(settings);
        if (mode == RmWriteBackModes.Off)
        {
            return new LeaveRmWriteBackResult(
                false,
                "pending_rm_sync",
                null,
                "Write-back RM desabilitado (leave.rm.writeback.mode=off). Solicitação permanece na fila do portal.");
        }

        var allowProd = settings.GetBool(AppSettingKeys.LeaveRmWriteBackAllowProd, false);
        if (!RmWriteBackModes.CanExecute(mode, allowProd))
        {
            return new LeaveRmWriteBackResult(
                false,
                "failed",
                null,
                "Modo \"apply\" bloqueado: habilite leave.rm.writeback.allow_prod para gravação definitiva no RM.");
        }

        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled || string.IsNullOrWhiteSpace(runtime.Password))
        {
            return new LeaveRmWriteBackResult(
                false,
                "failed",
                null,
                "Integração TOTVS RM não configurada/habilitada.");
        }

        if (command.Days <= 0)
        {
            return new LeaveRmWriteBackResult(false, "failed", null, "Quantidade de dias inválida para write-back.");
        }

        try
        {
            await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
            await connection.OpenAsync(cancellationToken);

            var openPeriod = await connection.QueryFirstOrDefaultAsync<OpenPeriodRow>(
                """
                SELECT TOP 1 FIMPERAQUIS AS FimPerAquis, CAST(ISNULL(SALDO, 0) AS int) AS Saldo
                FROM dbo.PFUFERIAS WITH (NOLOCK)
                WHERE CODCOLIGADA = @CodColigada
                  AND LTRIM(RTRIM(CHAPA)) = @Chapa
                  AND PERIODOABERTO = 1
                  AND SALDO >= @Dias
                ORDER BY FIMPERAQUIS ASC;
                """,
                new { CodColigada = TotvsRmConstants.CodColigada, command.Chapa, Dias = command.Days });

            if (openPeriod is null)
            {
                return new LeaveRmWriteBackResult(
                    false,
                    "failed",
                    null,
                    "Nenhum período aquisitivo aberto com saldo suficiente encontrado no RM.");
            }

            var marker = $"LIOWB-{command.RecordId:N}";
            var observacao = $"Solicitação portal LioConecta — {marker}";
            var (forwardSql, reverseSql) = BuildSql(command, openPeriod, observacao, marker);

            var sessionId = journalService.BeginSession();
            var rmKeysJson = JsonSerializer.Serialize(new
            {
                codColigada = TotvsRmConstants.CodColigada,
                chapa = command.Chapa,
                fimPerAquis = openPeriod.FimPerAquis,
                dataInicio = command.StartDate,
                dataFim = command.EndDate,
                marker,
            });

            if (mode == RmWriteBackModes.DryRun)
            {
                await journalService.AppendEntryAsync(
                    sessionId,
                    new RmWriteBackJournalEntryInput(
                        "leave", command.RecordId, marker, forwardSql, reverseSql, rmKeysJson, "dry_run"),
                    cancellationToken);

                return new LeaveRmWriteBackResult(
                    false,
                    "dry_run",
                    null,
                    $"dry_run: período {openPeriod.FimPerAquis:yyyy-MM-dd} validado (saldo {openPeriod.Saldo} ≥ {command.Days} dias). SQL não executado.");
            }

            await using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(forwardSql, transaction: transaction);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            await journalService.AppendEntryAsync(
                sessionId,
                new RmWriteBackJournalEntryInput(
                    "leave", command.RecordId, marker, forwardSql, reverseSql, rmKeysJson, "applied"),
                cancellationToken);

            var externalId = $"rm:{command.Chapa}:{command.StartDate:yyyyMMdd}:{command.EndDate:yyyyMMdd}";
            var rollbackHint = mode == RmWriteBackModes.ApplyRollbackable
                ? $" Sessão {sessionId:N} disponível para rollback (UAT)."
                : string.Empty;

            return new LeaveRmWriteBackResult(
                true,
                "synced",
                externalId,
                $"Férias programadas no RM (período aquisitivo {openPeriod.FimPerAquis:yyyy-MM-dd}).{rollbackHint}");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha no write-back SQL RM para férias {RecordId}.", command.RecordId);
            return new LeaveRmWriteBackResult(
                false,
                "failed",
                null,
                "Não foi possível gravar no RM agora. A solicitação permanece pendente de sincronização.");
        }
    }

    private static (string Forward, string Reverse) BuildSql(
        LeaveRmWriteBackCommand command,
        OpenPeriodRow period,
        string observacao,
        string marker)
    {
        var codColigada = TotvsRmConstants.CodColigada;
        var chapa = EscapeSql(command.Chapa);
        var fimPerAquis = FormatDate(period.FimPerAquis);
        var dataInicio = FormatDate(command.StartDate);
        var dataFim = FormatDate(command.EndDate);
        var observacaoLiteral = EscapeSql(observacao);
        var markerLiteral = EscapeSql(marker);

        var forward = $"""
            INSERT INTO dbo.PFUFERIASPER (
              CODCOLIGADA, CHAPA, FIMPERAQUIS,
              DATAINICIO, DATAFIM,
              NRODIASFERIAS, NRODIASFERIASCORRIDOS,
              NRODIASABONO, NRODIASABONOCORRIDOS, POSICAOABONO,
              PAGA1APARC13O, FERIASPERDIDAS, FALTAS,
              SITUACAOFERIAS, OBSERVACAO,
              RECCREATEDBY, RECCREATEDON
            ) VALUES (
              {codColigada}, '{chapa}', {fimPerAquis},
              {dataInicio}, {dataFim},
              {command.Days}, {command.Days},
              0, 0, 0,
              0, 0, 0,
              'P', '{observacaoLiteral}',
              'lioconecta', SYSUTCDATETIME()
            );

            UPDATE dbo.PFUFERIAS
            SET SALDO = SALDO - {command.Days},
                RECMODIFIEDBY = 'lioconecta',
                RECMODIFIEDON = SYSUTCDATETIME()
            WHERE CODCOLIGADA = {codColigada}
              AND LTRIM(RTRIM(CHAPA)) = '{chapa}'
              AND FIMPERAQUIS = {fimPerAquis}
              AND SALDO >= {command.Days};
            """;

        var reverse = $"""
            DELETE FROM dbo.PFUFERIASPER
            WHERE CODCOLIGADA = {codColigada}
              AND LTRIM(RTRIM(CHAPA)) = '{chapa}'
              AND FIMPERAQUIS = {fimPerAquis}
              AND DATAINICIO = {dataInicio}
              AND DATAFIM = {dataFim}
              AND OBSERVACAO LIKE '%{markerLiteral}%';

            UPDATE dbo.PFUFERIAS
            SET SALDO = SALDO + {command.Days},
                RECMODIFIEDBY = 'lioconecta-rollback',
                RECMODIFIEDON = SYSUTCDATETIME()
            WHERE CODCOLIGADA = {codColigada}
              AND LTRIM(RTRIM(CHAPA)) = '{chapa}'
              AND FIMPERAQUIS = {fimPerAquis};
            """;

        return (forward, reverse);
    }

    private static string EscapeSql(string value) => value.Replace("'", "''");

    private static string FormatDate(DateOnly date) => $"CONVERT(date, '{date:yyyy-MM-dd}', 23)";

    private static string FormatDate(DateTime date) => $"CONVERT(date, '{date:yyyy-MM-dd}', 23)";

    private sealed record OpenPeriodRow(DateTime FimPerAquis, int Saldo);
}
