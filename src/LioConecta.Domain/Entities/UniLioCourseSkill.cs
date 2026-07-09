using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioCourseSkill : BaseEntity
{
    public Guid CourseId { get; set; }

    public Guid SkillId { get; set; }

    public UniLioCourse Course { get; set; } = null!;

    public UniLioSkill Skill { get; set; } = null!;
}
