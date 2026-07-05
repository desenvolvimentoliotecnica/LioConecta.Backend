using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPrivate { get; set; }

    public Guid OwnerId { get; set; }

    public Person? Owner { get; set; }

    public ICollection<GroupMember> Members { get; set; } = [];

    public ICollection<GroupPost> Posts { get; set; } = [];
}
