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
