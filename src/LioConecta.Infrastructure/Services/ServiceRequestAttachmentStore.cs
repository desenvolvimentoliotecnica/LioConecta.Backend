using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class ServiceRequestAttachmentStore(IHostEnvironment hostEnvironment) : IServiceRequestAttachmentStore
{
    public const string PublicPathPrefix = "/service-requests/attachments";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
    };

    public async Task<ServiceRequestAttachmentMetaDto> SaveAsync(
        Stream content,
        string fileName,
        string? contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (sizeBytes <= 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        if (sizeBytes > ServiceRequestAttachmentLimits.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Arquivo excede o limite de 10 MB.");
        }

        var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "anexo" : fileName);
        var extension = Path.GetExtension(safeName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Tipo de arquivo não permitido. Use PDF, PNG ou JPG.");
        }

        var resolvedContentType = ResolveContentType(extension, contentType);
        if (!AllowedContentTypes.Contains(resolvedContentType))
        {
            throw new InvalidOperationException("Tipo de arquivo não permitido. Use PDF, PNG ou JPG.");
        }

        var storageRoot = ResolveRoot();
        Directory.CreateDirectory(storageRoot);

        var storageFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(storageRoot, storageFileName);

        await using (var output = File.Create(absolutePath))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        return new ServiceRequestAttachmentMetaDto(
            safeName,
            storageFileName,
            resolvedContentType,
            sizeBytes,
            $"{PublicPathPrefix}/{storageFileName}");
    }

    public string? ResolveAbsolutePath(string storageFileName)
    {
        if (string.IsNullOrWhiteSpace(storageFileName))
        {
            return null;
        }

        var safe = Path.GetFileName(storageFileName);
        var absolutePath = Path.Combine(ResolveRoot(), safe);
        return File.Exists(absolutePath) ? absolutePath : null;
    }

    private string ResolveRoot() =>
        Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "service-requests", "attachments");

    private static string ResolveContentType(string extension, string? uploadedContentType)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => string.IsNullOrWhiteSpace(uploadedContentType)
                ? "application/octet-stream"
                : uploadedContentType.Trim(),
        };
    }
}
