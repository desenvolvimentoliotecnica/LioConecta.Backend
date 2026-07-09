using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioModuleQuestionReply : BaseEntity
{
    public Guid QuestionId { get; set; }

    public Guid AuthorPersonId { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsInstructorReply { get; set; }

    public UniLioModuleQuestion Question { get; set; } = null!;

    public Person Author { get; set; } = null!;
}
