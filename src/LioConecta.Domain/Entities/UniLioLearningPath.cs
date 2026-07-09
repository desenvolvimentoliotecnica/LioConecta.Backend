using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioLearningPath : BaseEntity
{
    public string SeedKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<UniLioPathCourse> PathCourses { get; set; } = [];
}
