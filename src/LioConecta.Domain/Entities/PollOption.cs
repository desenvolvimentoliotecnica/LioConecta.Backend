using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class PollOption : BaseEntity
{
    public Guid PollId { get; set; }

    public Poll? Poll { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<PollVote> Votes { get; set; } = [];
}
