using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioAssessmentAttempt : BaseEntity
{
    public Guid PersonId { get; set; }

    public Guid AssessmentId { get; set; }

    public int Score { get; set; }

    public bool Passed { get; set; }

    public string AnswersJson { get; set; } = "{}";

    public DateTimeOffset AttemptedAt { get; set; }

    public Person Person { get; set; } = null!;

    public UniLioAssessment Assessment { get; set; } = null!;
}
