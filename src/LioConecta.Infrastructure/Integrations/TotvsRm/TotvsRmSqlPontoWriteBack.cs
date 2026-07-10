using System.Text.Json;
using Dapper;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.TotvsRm;

/// <summary>
/// Write-back SQL direto no TOTVS RM (Onda 1B): INSERT em ABATFUN por batida ajustada
/// (natureza 4 = entrada alterada pelo usuário, 5 = saída alterada), com journal reverso
/// para rollback. Ver docs/spike-writeback-sql-rm.md. AAFHTFUN (espelho processado) não é
/// escrito nesta onda — o sync RO continua lendo o espelho após o próximo processamento RM.
/// </summary>
public sealed class TotvsRmSqlPontoWriteBack(
    ITotvsRmConfigurationService configurationService,
    IRmWriteBackJournalService journalService,
    IAppSettingsProvider settings,
    ILogger<TotvsRmSqlPontoWriteBack> logger) : IPontoRmWriteBack
{
    private const int NaturezaEntrada = 4;
    private const int NaturezaSaida = 5;

    public async Task<PontoRmWriteBackResult> SubmitAdjustmentAsync(
        PontoRmWriteBackCommand command,
        CancellationToken cancellationToken = default)
    {
        var mode = RmWriteBackModes.ResolvePontoMode(settings);
        if (mode == RmWriteBackModes.Off)
        {
            return new PontoRmWriteBackResult(
                false,
                "pending_rm_sync",
                null,
                "Write-back RM desabilitado (ponto.rm.writeback.mode=off). Ajuste permanece na fila do portal.");
        }

        var allowProd = settings.GetBool(AppSettingKeys.PontoRmWriteBackAllowProd, false);
        if (!RmWriteBackModes.CanExecute(mode, allowProd))
        {
            return new PontoRmWriteBackResult(
                false,
                "failed",
                null,
                "Modo \"apply\" bloqueado: habilite ponto.rm.writeback.allow_prod para gravação definitiva no RM.");
        }

        var runtime = await configurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled || string.IsNullOrWhiteSpace(runtime.Password))
        {
            return new PontoRmWriteBackResult(false, "failed", null, "Integração TOTVS RM não configurada/habilitada.");
        }

        var punches = BuildPunches(command);
        if (punches.Count == 0)
        {
            return new PontoRmWriteBackResult(false, "failed", null, "Nenhuma batida válida informada para write-back.");
        }

        var sessionId = journalService.BeginSession();

        try
        {
            await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
            await connection.OpenAsync(cancellationToken);

            if (mode == RmWriteBackModes.DryRun)
            {
                foreach (var punch in punches)
                {
                    var (forward, reverse, rmKeysJson) = BuildSql(command, punch);
                    await journalService.AppendEntryAsync(
                        sessionId,
                        new RmWriteBackJournalEntryInput(
                            "ponto", command.RecordId, punch.Marker, forward, reverse, rmKeysJson, "dry_run"),
                        cancellationToken);
                }

                return new PontoRmWriteBackResult(
                    false,
                    "dry_run",
                    null,
                    $"dry_run: {punches.Count} batida(s) validada(s). SQL não executado.");
            }

            await using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var punch in punches)
                {
                    var (forward, _, _) = BuildSql(command, punch);
                    await connection.ExecuteAsync(forward, transaction: transaction);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            foreach (var punch in punches)
            {
                var (forward, reverse, rmKeysJson) = BuildSql(command, punch);
                await journalService.AppendEntryAsync(
                    sessionId,
                    new RmWriteBackJournalEntryInput(
                        "ponto", command.RecordId, punch.Marker, forward, reverse, rmKeysJson, "applied"),
                    cancellationToken);
            }

            var externalId = $"rm:{command.Chapa}:{sessionId:N}";
            var rollbackHint = mode == RmWriteBackModes.ApplyRollbackable
                ? $" Sessão {sessionId:N} disponível para rollback (UAT)."
                : string.Empty;

            return new PontoRmWriteBackResult(
                true,
                "synced",
                externalId,
                $"{punches.Count} batida(s) gravada(s) no RM (ABATFUN).{rollbackHint}");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Falha no write-back SQL RM para ajuste de ponto {RecordId}.",
                command.RecordId);

            return new PontoRmWriteBackResult(
                false,
                "failed",
                null,
                "Não foi possível gravar no RM agora. O ajuste permanece pendente de sincronização.");
        }
    }

    private static List<PunchEntry> BuildPunches(PontoRmWriteBackCommand command)
    {
        var punches = new List<PunchEntry>();
        var index = 0;

        foreach (var day in command.Days)
        {
            foreach (var (time, natureza) in new[]
                     {
                         (day.ClockIn, NaturezaEntrada),
                         (day.LunchOut, NaturezaSaida),
                         (day.LunchIn, NaturezaEntrada),
                         (day.ClockOut, NaturezaSaida),
                     })
            {
                var minutes = ParseMinutes(time);
                if (minutes is null)
                {
                    continue;
                }

                punches.Add(new PunchEntry(
                    day.Date,
                    minutes.Value,
                    natureza,
                    $"LIOWB-{command.RecordId:N}-{index++}"));
            }
        }

        return punches;
    }

    private static int? ParseMinutes(string? time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return null;
        }

        return TimeOnly.TryParse(time.Trim(), out var parsed) ? (parsed.Hour * 60) + parsed.Minute : null;
    }

    private static (string Forward, string Reverse, string RmKeysJson) BuildSql(
        PontoRmWriteBackCommand command,
        PunchEntry punch)
    {
        var codColigada = TotvsRmConstants.CodColigada;
        var chapa = EscapeSql(command.Chapa);
        var data = FormatDate(punch.Date);

        var forward = $"""
            INSERT INTO dbo.ABATFUN (
              CODCOLIGADA, CHAPA, DATA, BATIDA, STATUS, NATUREZA,
              DATAINSERCAO, RECCREATEDBY, RECCREATEDON, DATAREFERENCIAALTERADA
            ) VALUES (
              {codColigada}, '{chapa}', {data}, {punch.Minutes}, 'C', {punch.Natureza},
              SYSUTCDATETIME(), 'lioconecta', SYSUTCDATETIME(), 1
            );
            """;

        var reverse = $"""
            DELETE FROM dbo.ABATFUN
            WHERE CODCOLIGADA = {codColigada}
              AND LTRIM(RTRIM(CHAPA)) = '{chapa}'
              AND DATA = {data}
              AND BATIDA = {punch.Minutes}
              AND RECCREATEDBY = 'lioconecta'
              AND NATUREZA IN (4, 5);
            """;

        var rmKeysJson = JsonSerializer.Serialize(new
        {
            codColigada,
            chapa = command.Chapa,
            data = punch.Date,
            batida = punch.Minutes,
            natureza = punch.Natureza,
            marker = punch.Marker,
        });

        return (forward, reverse, rmKeysJson);
    }

    private static string EscapeSql(string value) => value.Replace("'", "''");

    private static string FormatDate(DateOnly date) => $"CONVERT(date, '{date:yyyy-MM-dd}', 23)";

    private sealed record PunchEntry(DateOnly Date, int Minutes, int Natureza, string Marker);
}
