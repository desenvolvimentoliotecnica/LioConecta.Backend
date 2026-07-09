using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioEnrollment : BaseEntity
{
    public Guid PersonId { get; set; }

    public Guid CourseId { get; set; }

    public string Status { get; set; } = "in_progress";

    public int ProgressPct { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset? CompletionNotifiedAt { get; set; }

    public int? CourseContentRating { get; set; }

    public string? CourseFeedbackComment { get; set; }

    public Person Person { get; set; } = null!;

    public UniLioCourse Course { get; set; } = null!;

    public ICollection<UniLioModuleProgress> ModuleProgress { get; set; } = [];
}
