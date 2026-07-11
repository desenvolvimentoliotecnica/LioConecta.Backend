using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public sealed class FeedbackSubmission : BaseEntity
{
    public Guid? AuthorId { get; set; }
    public Person? Author { get; set; }
    public bool IsAnonymous { get; set; }
    public FeedbackCategory Category { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Received;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
    public Guid? AssigneeId { get; set; }
    public Person? Assignee { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}
