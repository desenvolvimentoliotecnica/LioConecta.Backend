using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioCourseModule : BaseEntity
{
    public Guid CourseId { get; set; }

    public int SortOrder { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ContentType { get; set; } = "video";

    public string? ContentUrl { get; set; }

    public int DurationMinutes { get; set; }

    public string? ArticleHtml { get; set; }

    public string? QuizJson { get; set; }

    public UniLioCourse Course { get; set; } = null!;

    public ICollection<UniLioModuleProgress> ModuleProgress { get; set; } = [];
}
