using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class GroupPost : BaseEntity
{
    public Guid GroupId { get; set; }

    public Group? Group { get; set; }

    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public ICollection<GroupPostReaction> Reactions { get; set; } = [];
}
