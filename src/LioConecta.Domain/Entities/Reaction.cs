using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class Reaction : BaseEntity
{
    public Guid PostId { get; set; }

    public FeedPost? Post { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public string ReactionType { get; set; } = string.Empty;
}
