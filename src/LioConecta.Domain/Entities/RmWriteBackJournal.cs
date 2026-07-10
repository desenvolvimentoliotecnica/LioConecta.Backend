using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

/// <summary>
/// Registro de write-back SQL direto no TOTVS RM (Onda 1B), com SQL reverso para
/// permitir rollback (UAT/apply_rollbackable) ou auditoria (apply definitivo).
/// Ver docs/spike-writeback-sql-rm.md.
/// </summary>
public class RmWriteBackJournal : BaseEntity
{
    /// <summary>Agrupa entradas de uma mesma operação de write-back (permite rollback em lote).</summary>
    public Guid SessionId { get; set; }

    /// <summary>"leave" ou "ponto".</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Id do registro no portal (LeaveRecord.Id ou PontoAdjustmentRecord.Id).</summary>
    public Guid PortalRecordId { get; set; }

    /// <summary>Marcador único gravado no campo OBSERVACAO/RECCREATEDBY do RM para permitir localizar e revogar a linha.</summary>
    public string Marker { get; set; } = string.Empty;

    public string ForwardSql { get; set; } = string.Empty;

    public string ReverseSql { get; set; } = string.Empty;

    /// <summary>JSON com as chaves RM afetadas (CODCOLIGADA, CHAPA, FIMPERAQUIS, DATAINICIO, DATAFIM, etc.).</summary>
    public string RmKeysJson { get; set; } = "{}";

    /// <summary>"applied" | "rolled_back" | "dry_run".</summary>
    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? AppliedAt { get; set; }

    public DateTimeOffset? RolledBackAt { get; set; }
}
