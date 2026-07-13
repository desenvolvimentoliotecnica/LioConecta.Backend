using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioCourse : BaseEntity
{
    public string SeedKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ContentType { get; set; } = "video";

    public int DurationMinutes { get; set; }

    public bool IsMandatory { get; set; }

    public string Area { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public decimal Rating { get; set; }

    public string InstructorName { get; set; } = string.Empty;

    public Guid? InstructorPersonId { get; set; }

    public Person? InstructorPerson { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public Guid? SubmittedByPersonId { get; set; }

    public Guid? ReviewedById { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    public string? TagsJson { get; set; }

    public Guid? FeedPostId { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? ExternalUrl { get; set; }

    public string? Provider { get; set; }

    public string Status { get; set; } = "published";

    public string? VisibilityJson { get; set; }

    /// <summary>Nota mínima (0–100) para concluir curso SCORM e emitir certificado. Default 70.</summary>
    public int ScormPassingScore { get; set; } = 70;

    public ICollection<UniLioCourseModule> Modules { get; set; } = [];

    public ICollection<UniLioScormPackage> ScormPackages { get; set; } = [];

    public ICollection<UniLioCourseSkill> CourseSkills { get; set; } = [];

    public ICollection<UniLioEnrollment> Enrollments { get; set; } = [];

    public ICollection<UniLioAssessment> Assessments { get; set; } = [];

    public ICollection<UniLioIntegrationLink> IntegrationLinks { get; set; } = [];
}
