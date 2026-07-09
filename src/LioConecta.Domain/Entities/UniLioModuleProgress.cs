using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioModuleProgress : BaseEntity
{
    public Guid EnrollmentId { get; set; }

    public Guid ModuleId { get; set; }

    public DateTimeOffset CompletedAt { get; set; }

    public UniLioEnrollment Enrollment { get; set; } = null!;

    public UniLioCourseModule Module { get; set; } = null!;
}
