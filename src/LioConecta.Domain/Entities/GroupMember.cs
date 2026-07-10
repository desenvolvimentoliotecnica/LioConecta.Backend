using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class GroupMember : BaseEntity
{
    public Guid GroupId { get; set; }

    public Group? Group { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

    public DateTimeOffset JoinedAt { get; set; }
}
