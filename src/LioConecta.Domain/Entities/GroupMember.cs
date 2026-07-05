using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class GroupMember : BaseEntity
{
    public Guid GroupId { get; set; }

    public Group? Group { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}
