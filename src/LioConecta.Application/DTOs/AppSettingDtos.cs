namespace LioConecta.Application.DTOs;

public sealed class AppSettingDto
{
    public string Key { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Value { get; set; } = string.Empty;

    public string ValueType { get; set; } = "string";

    public bool IsSecret { get; set; }

    public bool HasValue { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AppSettingCategoryDto
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IReadOnlyList<AppSettingDto> Settings { get; set; } = [];
}

public sealed class UpdateAppSettingRequest
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public sealed class BulkUpdateAppSettingsRequest
{
    public IReadOnlyList<UpdateAppSettingRequest> Settings { get; set; } = [];
}

public sealed class AppSettingsUpdateResultDto
{
    public IReadOnlyList<AppSettingCategoryDto> Categories { get; set; } = [];

    public bool RequiresRestart { get; set; }

    public string? Message { get; set; }
}
