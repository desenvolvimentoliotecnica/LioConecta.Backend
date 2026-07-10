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
}
