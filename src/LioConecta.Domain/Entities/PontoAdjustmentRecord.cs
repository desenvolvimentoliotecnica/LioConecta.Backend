using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class PontoAdjustmentRecord : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    /// <summary>JSON: { items: [...], attachments: [...] }</summary>
    public string DetailsJson { get; set; } = "{}";

    public int DayCount { get; set; }

    public Guid? ServiceRequestId { get; set; }

    public string? DataSource { get; set; }

    /// <summary>"pending_rm_sync" | "synced" | "failed" | "dry_run" — write-back Onda 1B (ABATFUN).</summary>
    public string? RmSyncStatus { get; set; }

    public string? RmExternalId { get; set; }
}
