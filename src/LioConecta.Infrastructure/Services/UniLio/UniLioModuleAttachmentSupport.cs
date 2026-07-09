using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services.UniLio;

internal static class UniLioModuleAttachmentMapper
{
    public const string PublicPathPrefix = "/unilio/modules/attachments";

    public static IReadOnlyList<UniLioModuleAttachmentDto> Map(IEnumerable<UniLioModuleAttachment> attachments) =>
        attachments
            .OrderBy(a => a.SortOrder)
            .Select(Map)
            .ToList();

    public static UniLioModuleAttachmentDto Map(UniLioModuleAttachment attachment) =>
        new(
            attachment.Id,
            attachment.FileName,
            $"{PublicPathPrefix}/{attachment.StorageFileName}",
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.SortOrder);
}

internal static class UniLioModuleAttachmentStorage
{
    private const long MaxSizeBytes = 26_214_400;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".zip",
        ".rar",
        ".7z",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".csv",
    };

    public static string ResolveRoot(IHostEnvironment environment) =>
        Path.Combine(environment.ContentRootPath, "App_Data", "unilio", "module-attachments");

    public static void Validate(string fileName, long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        if (sizeBytes > MaxSizeBytes)
        {
            throw new InvalidOperationException("Arquivo excede o limite de 25 MB.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "Tipo de arquivo não permitido. Use PDF, ZIP, DOC, DOCX, XLS, XLSX, PPT, PPTX, TXT ou CSV.");
        }
    }

    public static async Task<string> SaveAsync(
        Stream content,
        string extension,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var storageFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(ResolveRoot(environment), storageFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var output = File.Create(absolutePath);
        await content.CopyToAsync(output, cancellationToken);
        return storageFileName;
    }

    public static void DeleteIfExists(string storageFileName, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(storageFileName))
        {
            return;
        }

        var absolutePath = Path.Combine(ResolveRoot(environment), storageFileName);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    public static string ResolveContentType(string fileName, string? uploadedContentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            _ => string.IsNullOrWhiteSpace(uploadedContentType) ? "application/octet-stream" : uploadedContentType,
        };
    }
}
