using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class WorkerRun : BaseEntity
{
    public string WorkerKey { get; set; } = string.Empty;

    public string Status { get; set; } = "running";

    public string TriggerSource { get; set; } = "scheduled";

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? FinishedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public ICollection<WorkerRunLog> Logs { get; set; } = [];
}
