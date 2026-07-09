using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioAssessment : BaseEntity
{
    public Guid CourseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int PassingScore { get; set; } = 70;

    public string QuestionsJson { get; set; } = "[]";

    public UniLioCourse Course { get; set; } = null!;

    public ICollection<UniLioAssessmentAttempt> Attempts { get; set; } = [];
}
