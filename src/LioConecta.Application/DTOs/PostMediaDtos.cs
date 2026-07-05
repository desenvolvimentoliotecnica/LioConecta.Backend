namespace LioConecta.Application.DTOs;

public sealed record UploadPostMediaResponseDto(
    string Url,
    string ContentType,
    string MediaType,
    long SizeBytes,
    string? OriginalFileName);
