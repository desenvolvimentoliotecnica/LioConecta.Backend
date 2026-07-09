using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioCommunityPost : BaseEntity
{
    public Guid AuthorPersonId { get; set; }

    public Guid? CourseId { get; set; }

    public string Body { get; set; } = string.Empty;

    public int LikesCount { get; set; }

    public Person Author { get; set; } = null!;

    public UniLioCourse? Course { get; set; }
}
