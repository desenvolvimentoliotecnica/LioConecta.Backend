using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public sealed class NewHireAnnouncement : BaseEntity
{
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public Guid FeedPostId { get; set; }
    public FeedPost? FeedPost { get; set; }
    public DateTimeOffset AnnouncedAt { get; set; }
}
