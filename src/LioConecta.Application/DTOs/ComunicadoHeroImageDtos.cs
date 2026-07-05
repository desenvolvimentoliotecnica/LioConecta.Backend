namespace LioConecta.Application.DTOs;

public sealed record ComunicadoHeroTemplateDto(
    string Id,
    string Label,
    string Url,
    string? Category);

public sealed record ComunicadoHeroUploadDto(
    Guid Id,
    Guid AssetId,
    int Version,
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    PersonSummaryDto? UploadedBy);

public sealed record UploadComunicadoHeroResponseDto(
    Guid Id,
    Guid AssetId,
    int Version,
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes);
