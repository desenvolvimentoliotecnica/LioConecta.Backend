namespace LioConecta.Application.Common;

public static class UniLioCourseStatuses
{
    public const string Draft = "draft";
    public const string PendingApproval = "pending_approval";
    public const string Published = "published";
    public const string Active = "active";
    public const string Rejected = "rejected";
    public const string Archived = "archived";

    public static readonly HashSet<string> LearnerVisible = new(StringComparer.OrdinalIgnoreCase)
    {
        Published,
        Active,
    };

    public static readonly HashSet<string> EditableByInstructor = new(StringComparer.OrdinalIgnoreCase)
    {
        Draft,
        Rejected,
    };
}
