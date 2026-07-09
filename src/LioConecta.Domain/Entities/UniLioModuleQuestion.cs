using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioModuleQuestion : BaseEntity
{
    public Guid CourseId { get; set; }

    public Guid? ModuleId { get; set; }

    public Guid AuthorPersonId { get; set; }

    public string Body { get; set; } = string.Empty;

    /// <summary>private | public</summary>
    public string Visibility { get; set; } = "private";

    /// <summary>open | answered | closed</summary>
    public string Status { get; set; } = "open";

    public DateTimeOffset? InstructorReadAt { get; set; }

    public DateTimeOffset? LearnerReadAt { get; set; }

    public UniLioCourse Course { get; set; } = null!;

    public UniLioCourseModule? Module { get; set; }

    public Person Author { get; set; } = null!;

    public ICollection<UniLioModuleQuestionReply> Replies { get; set; } = [];
}
