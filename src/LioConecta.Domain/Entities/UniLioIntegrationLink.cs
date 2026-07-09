using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioIntegrationLink : BaseEntity
{
    public string SourceType { get; set; } = string.Empty;

    public string SourceKey { get; set; } = string.Empty;

    public Guid CourseId { get; set; }

    public UniLioCourse Course { get; set; } = null!;
}
