using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class PostMediaService(
    IAppSettingsProvider settingsProvider,
    IHostEnvironment hostEnvironment) : IPostMediaService
{
    public async Task<UploadPostMediaResponseDto> UploadAsync(
        PostMediaUploadRequest request,
        Guid uploadedById,
        CancellationToken cancellationToken = default)
    {
        _ = uploadedById;

        if (request.SizeBytes <= 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        var maxSize = settingsProvider.GetInt(AppSettingKeys.MediaPostsMaxSizeBytes, 15_728_640);
        if (request.SizeBytes > maxSize)
        {
            throw new InvalidOperationException($"Arquivo excede o limite de {maxSize / 1_048_576} MB.");
        }

        var allowedTypes = settingsProvider.GetStringArray(AppSettingKeys.MediaPostsAllowedContentTypes);
        if (allowedTypes.Count == 0)
        {
            allowedTypes =
            [
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif",
                "video/mp4",
                "video/webm",
            ];
        }

        await using var buffered = new MemoryStream();
        await request.Content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        if (!TryDetectMediaType(buffered, out var detectedType, out var mediaType))
        {
            throw new InvalidOperationException("O arquivo não é uma imagem ou vídeo válido.");
        }

        if (!allowedTypes.Any(t => string.Equals(t, detectedType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Tipo de arquivo não permitido.");
        }

        var extension = ExtensionForContentType(detectedType);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(ResolveStorageRoot(), fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        buffered.Position = 0;
        await using (var output = File.Create(absolutePath))
        {
            await buffered.CopyToAsync(output, cancellationToken);
        }

        var publicUrl = $"/posts/medias/{fileName}";

        return new UploadPostMediaResponseDto(
            publicUrl,
            detectedType,
            mediaType,
            request.SizeBytes,
            Path.GetFileName(request.FileName));
    }

    private string ResolveStorageRoot()
    {
        var configured = settingsProvider.GetString(
            AppSettingKeys.MediaPostsRootPath,
            "App_Data/posts/medias");

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(hostEnvironment.ContentRootPath, configured);
    }

    private static string ExtensionForContentType(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "video/mp4" => ".mp4",
        "video/webm" => ".webm",
        _ => ".bin",
    };

    private static bool TryDetectMediaType(Stream stream, out string contentType, out string mediaType)
    {
        contentType = string.Empty;
        mediaType = string.Empty;

        if (!stream.CanSeek)
        {
            return false;
        }

        var start = stream.Position;
        Span<byte> header = stackalloc byte[16];
        var read = stream.Read(header);
        stream.Position = start;

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            contentType = "image/jpeg";
            mediaType = "image";
            return true;
        }

        if (read >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            contentType = "image/png";
            mediaType = "image";
            return true;
        }

        if (read >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            contentType = "image/webp";
            mediaType = "image";
            return true;
        }

        if (read >= 6 &&
            header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 &&
            header[3] == 0x38 && (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61)
        {
            contentType = "image/gif";
            mediaType = "image";
            return true;
        }

        if (read >= 12 &&
            header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p')
        {
            contentType = "video/mp4";
            mediaType = "video";
            return true;
        }

        if (read >= 4 &&
            header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3)
        {
            contentType = "video/webm";
            mediaType = "video";
            return true;
        }

        return false;
    }
}
