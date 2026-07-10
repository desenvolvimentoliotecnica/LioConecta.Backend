using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class GroupPostReaction : BaseEntity
{
    public Guid PostId { get; set; }

    public GroupPost? Post { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }
}
