using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class EmailAttachmentService(
    AppDbContext db,
    IHostEnvironment hostEnvironment) : IEmailAttachmentService
{
    public const int MaxAttachmentsPerSend = 5;
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan StagingTtl = TimeSpan.FromHours(24);

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "text/plain",
        "text/csv",
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    public async Task<EmailAttachmentUploadDto> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long sizeBytes,
        Guid uploadedById,
        CancellationToken cancellationToken)
    {
        if (sizeBytes <= 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        if (sizeBytes > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Arquivo excede o limite de 10 MB.");
        }

        var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "anexo" : fileName);
        var detectedType = DetectContentType(content, contentType, safeName);
        if (!AllowedContentTypes.Contains(detectedType))
        {
            throw new InvalidOperationException("Tipo de arquivo nao permitido.");
        }

        var storageRoot = ResolveStorageRoot();
        Directory.CreateDirectory(storageRoot);

        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(safeName)}";
        var absolutePath = Path.Combine(storageRoot, storedName);

        await using (var output = File.Create(absolutePath))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new EmailAttachmentStaging
        {
            Id = Guid.NewGuid(),
            FileName = safeName,
            ContentType = detectedType,
            StoragePath = absolutePath,
            SizeBytes = sizeBytes,
            CreatedById = uploadedById,
            ExpiresAt = now.Add(StagingTtl),
            IsConsumed = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.EmailAttachmentStagings.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new EmailAttachmentUploadDto(entity.Id, entity.FileName, entity.SizeBytes, entity.ContentType);
    }

    public async Task<IReadOnlyList<EmailAttachmentRecord>> ConsumeAsync(
        IReadOnlyList<Guid> attachmentIds,
        Guid uploadedById,
        CancellationToken cancellationToken)
    {
        if (attachmentIds is null || attachmentIds.Count == 0)
        {
            return Array.Empty<EmailAttachmentRecord>();
        }

        if (attachmentIds.Count > MaxAttachmentsPerSend)
        {
            throw new InvalidOperationException($"Maximo de {MaxAttachmentsPerSend} anexos por envio.");
        }

        var now = DateTimeOffset.UtcNow;
        var entities = await db.EmailAttachmentStagings
            .Where(a =>
                attachmentIds.Contains(a.Id) &&
                a.CreatedById == uploadedById &&
                !a.IsConsumed &&
                a.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        if (entities.Count != attachmentIds.Distinct().Count())
        {
            throw new InvalidOperationException("Um ou mais anexos sao invalidos ou expiraram.");
        }

        foreach (var entity in entities)
        {
            entity.IsConsumed = true;
            entity.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return entities
            .Select(a => new EmailAttachmentRecord(a.FileName, a.ContentType, a.StoragePath, a.SizeBytes))
            .ToList();
    }

    private string ResolveStorageRoot()
    {
        return Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "email", "attachments");
    }

    private static string DetectContentType(Stream content, string? declaredType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(declaredType) && AllowedContentTypes.Contains(declaredType))
        {
            return declaredType;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => declaredType ?? "application/octet-stream",
        };
    }
}
