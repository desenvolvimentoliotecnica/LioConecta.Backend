namespace LioConecta.Domain.Entities;

public class WorkerRunLog
{
    public Guid Id { get; set; }

    public Guid WorkerRunId { get; set; }

    public WorkerRun WorkerRun { get; set; } = null!;

    public DateTimeOffset LoggedAtUtc { get; set; }

    public string Level { get; set; } = "info";

    public string Message { get; set; } = string.Empty;
}
