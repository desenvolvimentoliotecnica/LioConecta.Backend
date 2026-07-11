using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record ComunicadoDto(
    Guid Id,
    string? Slug,
    ComunicadoKind Kind,
    string Title,
    string? Excerpt,
    IReadOnlyDictionary<string, object?> Content,
    PersonSummaryDto Author,
    string? HeroImageUrl,
    bool IsMandatory,
    ComunicadoStatus Status,
    DateTimeOffset? ScheduledAt,
    ComunicadoAudienceType AudienceType,
    IReadOnlyList<Guid> AudienceDepartmentIds,
    DateTimeOffset? PublishedAt,
    bool IsReadByViewer);

public sealed record ComunicadoListItemDto(
    Guid Id,
    string? Slug,
    ComunicadoKind Kind,
    string Title,
    string? Excerpt,
    PersonSummaryDto Author,
    string? HeroImageUrl,
    bool IsMandatory,
    ComunicadoStatus Status,
    DateTimeOffset? ScheduledAt,
    ComunicadoAudienceType AudienceType,
    IReadOnlyList<Guid> AudienceDepartmentIds,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ArchivedAt,
    bool IsReadByViewer);

public sealed record ComunicadoHubDto(
    int OficiaisCount,
    int DepartamentaisCount,
    int UrgentesCount,
    int UrgentesUnreadCount,
    int ArquivoCount,
    IReadOnlyList<ComunicadoListItemDto> Recent);

public sealed record ComunicadoMetricsDto(
    Guid ComunicadoId,
    int EligibleReaders,
    int ReadCount,
    decimal ReadPercent);

public sealed record CreateComunicadoRequest(
    ComunicadoKind Kind,
    string Title,
    string? Excerpt,
    IReadOnlyDictionary<string, object?>? Content,
    string? HeroImageUrl,
    bool IsMandatory,
    ComunicadoStatus Status = ComunicadoStatus.Published,
    DateTimeOffset? ScheduledAt = null,
    ComunicadoAudienceType AudienceType = ComunicadoAudienceType.All,
    IReadOnlyList<Guid>? AudienceDepartmentIds = null);

public sealed record UpdateComunicadoRequest(
    ComunicadoKind? Kind = null,
    string? Title = null,
    string? Excerpt = null,
    IReadOnlyDictionary<string, object?>? Content = null,
    string? HeroImageUrl = null,
    bool? IsMandatory = null,
    ComunicadoAudienceType? AudienceType = null,
    IReadOnlyList<Guid>? AudienceDepartmentIds = null);

public sealed record ScheduleComunicadoRequest(DateTimeOffset ScheduledAt);
