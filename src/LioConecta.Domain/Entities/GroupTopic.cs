using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class GroupTopic : BaseEntity
{
    public Guid GroupId { get; set; }

    public Group? Group { get; set; }

    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset LastActivityAt { get; set; }

    public ICollection<GroupTopicReply> Replies { get; set; } = [];
}
