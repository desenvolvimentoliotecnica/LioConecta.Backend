using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioPersonSkill : BaseEntity
{
    public Guid PersonId { get; set; }

    public Guid SkillId { get; set; }

    public int Level { get; set; }

    public Person Person { get; set; } = null!;

    public UniLioSkill Skill { get; set; } = null!;
}
