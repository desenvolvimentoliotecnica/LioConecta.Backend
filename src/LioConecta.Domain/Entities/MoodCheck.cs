using LioConecta.Domain.Common;
using LioConecta.Domain.Enums;

namespace LioConecta.Domain.Entities;

public class MoodCheck : BaseEntity
{
    public Guid PersonId { get; set; }

    public Person? Person { get; set; }

    public MoodLevel Mood { get; set; }

    /// <summary>Calendar date in company timezone (America/Sao_Paulo) for once-per-day rule.</summary>
    public DateOnly CheckDate { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
