namespace LioConecta.Application.Interfaces.Services;

public sealed record RmWriteBackJournalEntryInput(
    string Domain,
    Guid PortalRecordId,
    string Marker,
    string ForwardSql,
    string ReverseSql,
    string RmKeysJson,
    string Status);

public sealed record RmWriteBackRollbackResult(
    bool Success,
    int EntriesRolledBack,
    string Message);

/// <summary>
/// Persiste o journal de write-back SQL direto no TOTVS RM (forward/reverse SQL por
/// sessão) e executa rollback quando solicitado. Ver docs/spike-writeback-sql-rm.md.
/// </summary>
public interface IRmWriteBackJournalService
{
    /// <summary>Abre uma nova sessão de write-back (agrupa entradas relacionadas para rollback em lote).</summary>
    Guid BeginSession();

    Task AppendEntryAsync(
        Guid sessionId,
        RmWriteBackJournalEntryInput entry,
        CancellationToken cancellationToken = default);

    Task<RmWriteBackRollbackResult> RollbackSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
