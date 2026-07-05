using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class Poll : BaseEntity
{
    public Guid PostId { get; set; }

    public FeedPost? Post { get; set; }

    public string Question { get; set; } = string.Empty;

    public DateTimeOffset? EndsAt { get; set; }

    public ICollection<PollOption> Options { get; set; } = [];
}
