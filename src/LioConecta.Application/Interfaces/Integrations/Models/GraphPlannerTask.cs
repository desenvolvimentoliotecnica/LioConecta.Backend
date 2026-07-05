namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GraphPlannerTask
{
    public string TaskId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? BucketName { get; set; }

    public int PercentComplete { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}
