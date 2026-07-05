using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class Celebration : BaseEntity
{
    public Guid PostId { get; set; }

    public FeedPost? Post { get; set; }

    public Guid CelebratedPersonId { get; set; }

    public Person? CelebratedPerson { get; set; }

    public string Message { get; set; } = string.Empty;
}
