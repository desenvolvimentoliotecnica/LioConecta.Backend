namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class GlpiFormCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CompleteName { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
}

public sealed class GlpiFormSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Illustration { get; set; }
    public int CategoryId { get; set; }
    public string RenderLayout { get; set; } = "single_page";
}

public sealed class GlpiFormSchema
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public IReadOnlyList<GlpiFormSection> Sections { get; set; } = [];
}

public sealed class GlpiFormSection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rank { get; set; }
    public IReadOnlyList<GlpiFormQuestion> Questions { get; set; } = [];
}

public sealed class GlpiFormQuestion
{
    public int Id { get; set; }
    public string Uuid { get; set; } = string.Empty;
    public int SectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public int VerticalRank { get; set; }
    public int? HorizontalRank { get; set; }
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
    public string? ExtraDataJson { get; set; }
    public string VisibilityStrategy { get; set; } = string.Empty;
    public string ConditionsJson { get; set; } = "[]";
}

public sealed class GlpiFormAnswerInput
{
    public int QuestionId { get; set; }
    public string Value { get; set; } = string.Empty;
}
