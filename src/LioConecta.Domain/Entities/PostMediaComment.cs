using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class PostMediaComment : BaseEntity
{
    public Guid PostId { get; set; }

    public FeedPost? Post { get; set; }

    public string MediaUrl { get; set; } = string.Empty;

    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public string Text { get; set; } = string.Empty;
}
