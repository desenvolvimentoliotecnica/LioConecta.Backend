namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class PlannerGraphPlan
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}

public sealed class PlannerGraphBucket
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? OrderHint { get; set; }
}

public sealed class PlannerGraphChecklistItem
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public bool IsChecked { get; set; }
}

public sealed class PlannerGraphTask
{
    public string Id { get; set; } = string.Empty;

    public string PlanId { get; set; } = string.Empty;

    public string BucketId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int PercentComplete { get; set; }

    public DateTimeOffset? StartDateTime { get; set; }

    public DateTimeOffset? DueDateTime { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public DateTimeOffset? CompletedDateTime { get; set; }

    public IReadOnlyList<string> AssigneeIds { get; set; } = [];

    public string Etag { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<PlannerGraphChecklistItem> Checklist { get; set; } = [];
}

public sealed class PlannerGraphUser
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? Mail { get; set; }

    public string? UserPrincipalName { get; set; }
}
