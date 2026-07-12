using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class FeedPost : BaseEntity
{
    public Guid AuthorId { get; set; }

    public Person? Author { get; set; }

    public PostType Type { get; set; }

    public string Content { get; set; } = string.Empty;

    public string MetadataJson { get; set; } = "{}";

    public bool IsPinned { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// When set in the future, the post stays hidden from feed/news until published by the schedule worker.
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; set; }

    public ICollection<Comment> Comments { get; set; } = [];

    public ICollection<Reaction> Reactions { get; set; } = [];
}
