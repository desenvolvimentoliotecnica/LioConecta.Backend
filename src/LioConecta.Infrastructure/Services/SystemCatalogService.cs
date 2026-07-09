using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace LioConecta.Infrastructure.Services;

public sealed class SystemCatalogService(
    AppDbContext db,
    IPermissionService permissionService,
    IAppSettingsProvider settingsProvider,
    IHostEnvironment hostEnvironment) : ISystemCatalogService
{
    private const long MaxIconSizeBytes = 2_097_152;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] AllowedIconContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "image/svg+xml",
    ];

    public async Task<SystemsBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var canManage = await CanManageAsync(cancellationToken);
        var query = db.PortalSystems.AsNoTracking().Where(x => x.IsActive);
        var total = await query.CountAsync(cancellationToken);
        var categories = await query
            .Select(x => x.Category)
            .Where(category => category != null && category != "")
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);

        return new SystemsBootstrapDto(canManage, ResolveEnvironment(), total, categories);
    }

    public async Task<PortalSystemManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default)
        => new(await CanManageAsync(cancellationToken));

    public async Task<IReadOnlyList<PortalSystemDto>> ListAsync(
        string? q = null,
        string? category = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var environment = ResolveEnvironment();
        var query = db.PortalSystems.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim();
            query = query.Where(x => x.Category == normalizedCategory);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(term)
                || x.Slug.ToLower().Contains(term)
                || (x.Description != null && x.Description.ToLower().Contains(term))
                || x.Category.ToLower().Contains(term)
                || (x.AccessNotes != null && x.AccessNotes.ToLower().Contains(term)));
        }

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items.Select(item => Map(item, environment)).ToList();
    }

    public async Task<PortalSystemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.PortalSystems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity, ResolveEnvironment());
    }

    public async Task<PortalSystemDto> CreateAsync(UpsertPortalSystemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(cancellationToken);
        ValidateRequest(request);
        await EnsureSlugAvailableAsync(request.Slug, null, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new PortalSystem
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = NormalizeSlug(request.Slug),
            Description = NormalizeOptional(request.Description),
            Category = request.Category.Trim(),
            DestinationType = ParseDestinationType(request.DestinationType),
            UrlDev = NormalizeOptional(request.UrlDev),
            UrlHml = NormalizeOptional(request.UrlHml),
            UrlPrd = NormalizeOptional(request.UrlPrd),
            IconKind = ParseIconKind(request.IconKind),
            IconFaClass = NormalizeIconFaClass(request.IconFaClass),
            IconAssetUrl = NormalizeOptional(request.IconAssetUrl),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            AccessNotes = NormalizeOptional(request.AccessNotes),
            ClickCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.PortalSystems.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<PortalSystemDto> UpdateAsync(Guid id, UpsertPortalSystemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(cancellationToken);
        ValidateRequest(request);
        await EnsureSlugAvailableAsync(request.Slug, id, cancellationToken);

        var entity = await db.PortalSystems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sistema {id} nao encontrado.");

        entity.Name = request.Name.Trim();
        entity.Slug = NormalizeSlug(request.Slug);
        entity.Description = NormalizeOptional(request.Description);
        entity.Category = request.Category.Trim();
        entity.DestinationType = ParseDestinationType(request.DestinationType);
        entity.UrlDev = NormalizeOptional(request.UrlDev);
        entity.UrlHml = NormalizeOptional(request.UrlHml);
        entity.UrlPrd = NormalizeOptional(request.UrlPrd);
        entity.IconKind = ParseIconKind(request.IconKind);
        entity.IconFaClass = NormalizeIconFaClass(request.IconFaClass);
        entity.IconAssetUrl = NormalizeOptional(request.IconAssetUrl);
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.AccessNotes = NormalizeOptional(request.AccessNotes);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(cancellationToken);
        var entity = await db.PortalSystems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sistema {id} nao encontrado.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordClickAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.PortalSystems.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (entity is null)
        {
            throw new KeyNotFoundException($"Sistema {id} nao encontrado.");
        }

        entity.ClickCount += 1;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UploadSystemIconResponseDto> UploadIconAsync(
        Guid id,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(cancellationToken);

        if (sizeBytes <= 0)
        {
            throw new InvalidOperationException("Arquivo vazio.");
        }

        if (sizeBytes > MaxIconSizeBytes)
        {
            throw new InvalidOperationException("Arquivo excede o limite de 2 MB.");
        }

        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim().ToLowerInvariant();

        if (!AllowedIconContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Tipo de arquivo nao permitido. Use JPEG, PNG, WebP, GIF ou SVG.");
        }

        var entity = await db.PortalSystems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sistema {id} nao encontrado.");

        var extension = ExtensionForContentType(normalizedContentType);
        var storedFileName = $"{entity.Slug}-{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(ResolveIconsStorageRoot(), storedFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await using (var output = File.Create(absolutePath))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        var publicUrl = $"/systems/icons/{storedFileName}";
        entity.IconKind = PortalSystemIconKind.Upload;
        entity.IconAssetUrl = publicUrl;
        entity.IconFaClass = null;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new UploadSystemIconResponseDto(publicUrl, normalizedContentType, sizeBytes, Path.GetFileName(fileName));
    }

    private Task<bool> CanManageAsync(CancellationToken cancellationToken) =>
        permissionService.HasPermissionAsync("systems.manage", DataScope.Global, cancellationToken);

    private async Task EnsureCanManageAsync(CancellationToken cancellationToken)
    {
        await permissionService.EnsurePermissionAsync("systems.manage", DataScope.Global, cancellationToken);
    }

    private async Task EnsureSlugAvailableAsync(string slug, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSlug(slug);
        var exists = await db.PortalSystems
            .AsNoTracking()
            .AnyAsync(
                x => x.Slug == normalized && (currentId == null || x.Id != currentId.Value),
                cancellationToken);

        if (exists)
        {
            throw new ArgumentException("Ja existe um sistema com este slug.");
        }
    }

    private static void ValidateRequest(UpsertPortalSystemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Nome e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            throw new ArgumentException("Slug e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ArgumentException("Categoria e obrigatoria.");
        }

        var destinationType = ParseDestinationType(request.DestinationType);
        var hasUrl = !string.IsNullOrWhiteSpace(request.UrlDev)
            || !string.IsNullOrWhiteSpace(request.UrlHml)
            || !string.IsNullOrWhiteSpace(request.UrlPrd);

        if (!hasUrl)
        {
            throw new ArgumentException("Informe ao menos uma URL de ambiente.");
        }

        var iconKind = ParseIconKind(request.IconKind);
        if (iconKind == PortalSystemIconKind.FontAwesome && string.IsNullOrWhiteSpace(request.IconFaClass))
        {
            throw new ArgumentException("Selecione um icone Font Awesome.");
        }

        if (destinationType == PortalSystemDestinationType.Internal)
        {
            var internalUrl = request.UrlPrd ?? request.UrlHml ?? request.UrlDev;
            if (internalUrl is not null && !internalUrl.TrimStart().StartsWith('/'))
            {
                throw new ArgumentException("Rotas internas devem comecar com '/'.");
            }
        }
    }

    private string ResolveEnvironment()
    {
        var raw = settingsProvider.GetString(AppSettingKeys.PortalEnvironment, "prd").Trim().ToLowerInvariant();
        return raw switch
        {
            "dev" => "dev",
            "hml" => "hml",
            _ => "prd",
        };
    }

    private string ResolveIconsStorageRoot()
    {
        var configured = settingsProvider.GetString(
            AppSettingKeys.SystemsIconsRootPath,
            "App_Data/systems/icons");

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(hostEnvironment.ContentRootPath, configured);
    }

    private static PortalSystemDto Map(PortalSystem entity, string environment) =>
        new(
            entity.Id,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.Category,
            entity.DestinationType.ToString(),
            entity.UrlDev,
            entity.UrlHml,
            entity.UrlPrd,
            ResolveLaunchUrl(entity, environment),
            entity.IconKind.ToString(),
            entity.IconFaClass,
            entity.IconAssetUrl,
            entity.SortOrder,
            entity.IsActive,
            entity.AccessNotes,
            entity.ClickCount,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static string? ResolveLaunchUrl(PortalSystem entity, string environment)
    {
        var primary = environment switch
        {
            "dev" => entity.UrlDev,
            "hml" => entity.UrlHml,
            _ => entity.UrlPrd,
        };

        return primary
            ?? entity.UrlPrd
            ?? entity.UrlHml
            ?? entity.UrlDev;
    }

    private static PortalSystemDestinationType ParseDestinationType(string raw)
    {
        if (Enum.TryParse<PortalSystemDestinationType>(raw, true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException("Tipo de destino invalido.");
    }

    private static PortalSystemIconKind ParseIconKind(string raw)
    {
        if (Enum.TryParse<PortalSystemIconKind>(raw, true, out var parsed))
        {
            return parsed;
        }

        return PortalSystemIconKind.FontAwesome;
    }

    private static string NormalizeSlug(string value) =>
        value.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeIconFaClass(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.StartsWith("fa-", StringComparison.Ordinal) ? trimmed : $"fa-{trimmed}";
    }

    private static string ExtensionForContentType(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "image/svg+xml" => ".svg",
        _ => ".bin",
    };
}
