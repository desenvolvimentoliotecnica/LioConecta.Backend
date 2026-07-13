using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioScormAttempt : BaseEntity
{
    public Guid EnrollmentId { get; set; }

    public Guid CourseId { get; set; }

    public Guid ModuleId { get; set; }

    public Guid PackageId { get; set; }

    public string LessonStatus { get; set; } = "not attempted";

    public decimal? ScoreRaw { get; set; }

    public decimal? ScoreMin { get; set; }

    public decimal? ScoreMax { get; set; }

    public string? SessionTime { get; set; }

    public string? LessonLocation { get; set; }

    public string? SuspendData { get; set; }

    public string? CmiJson { get; set; }

    public DateTimeOffset? InitializedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    public UniLioEnrollment Enrollment { get; set; } = null!;

    public UniLioScormPackage Package { get; set; } = null!;
}
