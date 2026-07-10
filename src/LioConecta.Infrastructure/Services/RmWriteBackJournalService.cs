using Dapper;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Integrations.TotvsRm;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

/// <summary>
/// Implementação Postgres do journal de write-back RM (Onda 1B). Guarda SQL
/// forward/reverse por sessão e executa o reverse contra o TOTVS RM no rollback.
/// </summary>
public sealed class RmWriteBackJournalService(
    AppDbContext db,
    ITotvsRmConfigurationService totvsRmConfigurationService,
    ILogger<RmWriteBackJournalService> logger) : IRmWriteBackJournalService
{
    public Guid BeginSession() => Guid.NewGuid();

    public async Task AppendEntryAsync(
        Guid sessionId,
        RmWriteBackJournalEntryInput entry,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var journal = new RmWriteBackJournal
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Domain = entry.Domain,
            PortalRecordId = entry.PortalRecordId,
            Marker = entry.Marker,
            ForwardSql = entry.ForwardSql,
            ReverseSql = entry.ReverseSql,
            RmKeysJson = entry.RmKeysJson,
            Status = entry.Status,
            AppliedAt = entry.Status is "applied" ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.RmWriteBackJournals.Add(journal);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RmWriteBackRollbackResult> RollbackSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var entries = await db.RmWriteBackJournals
            .Where(j => j.SessionId == sessionId && (j.Status == "applied" || j.Status == "dry_run"))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return new RmWriteBackRollbackResult(false, 0, "Sessão não encontrada ou já revertida.");
        }

        var runtime = await totvsRmConfigurationService.GetRuntimeConfigurationAsync(cancellationToken);
        if (!runtime.IsEnabled || string.IsNullOrWhiteSpace(runtime.Password))
        {
            return new RmWriteBackRollbackResult(false, 0, "Integração TOTVS RM indisponível para rollback.");
        }

        var rolledBack = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.Status is "dry_run")
            {
                entry.Status = "rolled_back";
                entry.RolledBackAt = now;
                entry.UpdatedAt = now;
                rolledBack++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.ReverseSql))
            {
                continue;
            }

            try
            {
                await using var connection = TotvsRmConnectionFactory.CreateConnection(runtime);
                await connection.OpenAsync(cancellationToken);
                await connection.ExecuteAsync(entry.ReverseSql);

                entry.Status = "rolled_back";
                entry.RolledBackAt = now;
                entry.UpdatedAt = now;
                rolledBack++;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Falha ao reverter write-back RM {JournalId} (sessão {SessionId}, domínio {Domain}).",
                    entry.Id,
                    sessionId,
                    entry.Domain);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return rolledBack == entries.Count
            ? new RmWriteBackRollbackResult(true, rolledBack, $"Rollback concluído: {rolledBack} registro(s) revertido(s).")
            : new RmWriteBackRollbackResult(
                rolledBack > 0,
                rolledBack,
                $"Rollback parcial: {rolledBack} de {entries.Count} registro(s) revertido(s). Verifique os logs.");
    }
}
