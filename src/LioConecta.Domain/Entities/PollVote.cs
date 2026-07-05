using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class PollVote : BaseEntity
{
    public Guid PollOptionId { get; set; }

    public PollOption? PollOption { get; set; }

    public Guid PersonId { get; set; }

    public Person? Person { get; set; }
}
