using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioPathCourse : BaseEntity
{
    public Guid PathId { get; set; }

    public Guid CourseId { get; set; }

    public int SortOrder { get; set; }

    public UniLioLearningPath Path { get; set; } = null!;

    public UniLioCourse Course { get; set; } = null!;
}
