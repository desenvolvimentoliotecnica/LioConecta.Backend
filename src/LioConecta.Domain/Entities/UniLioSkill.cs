using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioSkill : BaseEntity
{
    public string SeedKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<UniLioCourseSkill> CourseSkills { get; set; } = [];

    public ICollection<UniLioPersonSkill> PersonSkills { get; set; } = [];
}
