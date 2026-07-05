using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class TimesheetPeriodCache : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public int Year { get; set; }

    public int Month { get; set; }

    public string SummaryJson { get; set; } = "{}";

    public string EntriesJson { get; set; } = "[]";

    public DateTimeOffset SyncedAtUtc { get; set; }

    public string Source { get; set; } = "totvs-rm";
}
