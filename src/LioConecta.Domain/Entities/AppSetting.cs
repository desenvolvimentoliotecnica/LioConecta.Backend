using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class AppSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Value { get; set; } = string.Empty;

    /// <summary>string | boolean | integer | json | secret</summary>
    public string ValueType { get; set; } = "string";

    public bool IsSecret { get; set; }

    public int SortOrder { get; set; }

    public Guid? UpdatedById { get; set; }

    public Person? UpdatedBy { get; set; }
}
