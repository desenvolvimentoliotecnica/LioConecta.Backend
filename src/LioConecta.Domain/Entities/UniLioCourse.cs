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

    public string? ThumbnailUrl { get; set; }

    public string? ExternalUrl { get; set; }

    public string? Provider { get; set; }

    public string Status { get; set; } = "published";

    public string? VisibilityJson { get; set; }

    public ICollection<UniLioCourseModule> Modules { get; set; } = [];

    public ICollection<UniLioCourseSkill> CourseSkills { get; set; } = [];

    public ICollection<UniLioEnrollment> Enrollments { get; set; } = [];

    public ICollection<UniLioAssessment> Assessments { get; set; } = [];

    public ICollection<UniLioIntegrationLink> IntegrationLinks { get; set; } = [];
}
