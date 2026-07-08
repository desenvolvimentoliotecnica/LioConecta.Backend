namespace LioConecta.Application.DTOs;

public sealed record PortalSystemDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Category,
    string DestinationType,
    string? UrlDev,
    string? UrlHml,
    string? UrlPrd,
    string? LaunchUrl,
    string IconKind,
    string? IconFaClass,
    string? IconAssetUrl,
    int SortOrder,
    bool IsActive,
    string? AccessNotes,
    long ClickCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SystemsBootstrapDto(
    bool CanManage,
    string Environment,
    int Total,
    IReadOnlyList<string> Categories);

public sealed record UpsertPortalSystemRequest(
    string Name,
    string Slug,
    string? Description,
    string Category,
    string DestinationType,
    string? UrlDev,
    string? UrlHml,
    string? UrlPrd,
    string IconKind,
    string? IconFaClass,
    string? IconAssetUrl,
    int SortOrder,
    bool IsActive = true,
    string? AccessNotes = null);

public sealed record PortalSystemManagePolicyDto(bool CanManage);

public sealed record UploadSystemIconResponseDto(
    string Url,
    string ContentType,
    long SizeBytes,
    string? OriginalFileName);
