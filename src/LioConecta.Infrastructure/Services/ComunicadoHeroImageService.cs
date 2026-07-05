using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class ComunicadoHeroImageService(
    IComunicadoHeroImageRepository repository,
    IAppSettingsProvider settingsProvider,
    IHostEnvironment hostEnvironment) : IComunicadoHeroImageService
{
    public IReadOnlyList<ComunicadoHeroTemplateDto> GetTemplates() =>
        ComunicadoHeroTemplateCatalog.All
            .Select(t => new ComunicadoHeroTemplateDto(t.Id, t.Label, t.Url, t.Category))
            .ToList();

    public async Task<IReadOnlyList<ComunicadoHeroUploadDto>> GetRecentUploadsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetRecentAsync(Math.Clamp(limit, 1, 100), cancellationToken);
        return rows.Select(MapUpload).ToList();
    }

    public async Task<UploadComunicadoHeroResponseDto> UploadAsync(
        ComunicadoHeroUploadRequest request,
        Guid uploadedById,
        CancellationToken cancellationToken = default)
    {
        if (request.SizeBytes <= 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        var maxSize = settingsProvider.GetInt(AppSettingKeys.MediaComunicadosMaxSizeBytes, 5_242_880);
        if (request.SizeBytes > maxSize)
        {
            throw new InvalidOperationException($"Arquivo excede o limite de {maxSize / 1_048_576} MB.");
        }

        var allowedTypes = settingsProvider.GetStringArray(AppSettingKeys.MediaComunicadosAllowedContentTypes);
        if (allowedTypes.Count == 0)
        {
            allowedTypes = ["image/jpeg", "image/png", "image/webp"];
        }

        var normalizedContentType = request.ContentType.Trim().ToLowerInvariant();
        if (!allowedTypes.Any(t => string.Equals(t, normalizedContentType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tipo de arquivo não permitido.");
        }

        await using var buffered = new MemoryStream();
        await request.Content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        if (!TryDetectImageType(buffered, out var detectedType))
        {
            throw new InvalidOperationException("O arquivo não é uma imagem válida.");
        }

        if (!allowedTypes.Any(t => string.Equals(t, detectedType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tipo de imagem não permitido.");
        }

        var assetId = request.AssetId ?? Guid.NewGuid();
        var nextVersion = await repository.GetMaxVersionAsync(assetId, cancellationToken) + 1;
        var extension = ExtensionForContentType(detectedType);
        var shortId = Guid.NewGuid().ToString("N")[..8];
        var fileStem = $"v{nextVersion}-{shortId}";
        var relativePath = Path.Combine("uploads", assetId.ToString("N"), fileStem + extension);
        var absolutePath = Path.Combine(ResolveStorageRoot(), relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        buffered.Position = 0;
        await using (var output = File.Create(absolutePath))
        {
            await buffered.CopyToAsync(output, cancellationToken);
        }

        var publicUrl = $"/media/comunicados/{relativePath.Replace('\\', '/')}";
        var now = DateTimeOffset.UtcNow;
        var entity = new ComunicadoHeroImage
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Version = nextVersion,
            StoragePath = relativePath.Replace('\\', '/'),
            PublicUrl = publicUrl,
            OriginalFileName = Path.GetFileName(request.FileName),
            ContentType = detectedType,
            SizeBytes = request.SizeBytes,
            UploadedById = uploadedById,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddAsync(entity, cancellationToken);

        return new UploadComunicadoHeroResponseDto(
            entity.Id,
            entity.AssetId,
            entity.Version,
            entity.PublicUrl,
            entity.OriginalFileName,
            entity.ContentType,
            entity.SizeBytes);
    }

    private string ResolveStorageRoot()
    {
        var configured = settingsProvider.GetString(
            AppSettingKeys.MediaComunicadosRootPath,
            "App_Data/media/comunicados");

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(hostEnvironment.ContentRootPath, configured);
    }

    private static ComunicadoHeroUploadDto MapUpload(ComunicadoHeroImage row) =>
        new(
            row.Id,
            row.AssetId,
            row.Version,
            row.PublicUrl,
            row.OriginalFileName,
            row.ContentType,
            row.SizeBytes,
            row.CreatedAt,
            row.UploadedBy is null ? null : PersonMapper.ToSummary(row.UploadedBy));

    private static string ExtensionForContentType(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".bin",
    };

    private static bool TryDetectImageType(Stream stream, out string contentType)
    {
        contentType = string.Empty;
        if (!stream.CanSeek)
        {
            return false;
        }

        var start = stream.Position;
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        stream.Position = start;

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            contentType = "image/jpeg";
            return true;
        }

        if (read >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            contentType = "image/png";
            return true;
        }

        if (read >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            contentType = "image/webp";
            return true;
        }

        return false;
    }
}
