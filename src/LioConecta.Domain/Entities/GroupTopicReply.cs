using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class GroupTopicReply : BaseEntity
{
    public Guid TopicId { get; set; }

    public GroupTopic? Topic { get; set; }

    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public string Body { get; set; } = string.Empty;
}
