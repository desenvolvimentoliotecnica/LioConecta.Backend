using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Services;

public sealed class BenefitCatalogService(
    IBenefitCatalogRepository catalogRepository,
    IPermissionService permissionService) : IBenefitCatalogService
{
    public async Task<BenefitManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default) =>
        new(await BenefitManageAuthorization.CanManageAsync(permissionService, cancellationToken));

    public async Task<IReadOnlyList<BenefitCatalogItemDto>> ListAsync(
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var items = await catalogRepository.ListAsync(q, category, includeInactive, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<BenefitCatalogItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await catalogRepository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<BenefitCatalogItemDto> CreateAsync(
        UpsertBenefitCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(permissionService, cancellationToken);
        ValidateCatalogRequest(request);

        var entityId = Guid.NewGuid();
        var catalogKey = await ResolveCatalogKeyAsync(
            request.CatalogKey,
            request.Title,
            entityId,
            excludeId: null,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = MapToEntity(request, entityId, now, catalogKey);
        await catalogRepository.AddAsync(entity, cancellationToken);
        await catalogRepository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<BenefitCatalogItemDto> UpdateAsync(
        Guid id,
        UpsertBenefitCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(permissionService, cancellationToken);
        ValidateCatalogRequest(request);

        var entity = await catalogRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Item de catálogo {id} não encontrado.");

        ApplyCatalogRequest(entity, request);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await catalogRepository.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(permissionService, cancellationToken);
        var entity = await catalogRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Item de catálogo {id} não encontrado.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await catalogRepository.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCatalogRequest(UpsertBenefitCatalogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Título é obrigatório.");
        }

        if (!BenefitManageAuthorization.Categories.Contains(request.Category))
        {
            throw new ArgumentException("Categoria inválida.");
        }

        if (!BenefitManageAuthorization.Statuses.Contains(request.Status))
        {
            throw new ArgumentException("Status inválido.");
        }
    }

    private static string NormalizeKey(string key) => key.Trim().ToLowerInvariant();

    private async Task<string> ResolveCatalogKeyAsync(
        string? requestedKey,
        string title,
        Guid fallbackId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var baseKey = !string.IsNullOrWhiteSpace(requestedKey)
            ? NormalizeKey(requestedKey)
            : PersonSlugHelper.Slugify(title);

        if (string.IsNullOrWhiteSpace(baseKey) || baseKey == "sem-nome")
        {
            baseKey = SlugHelper.FromTitle(title, fallbackId);
        }

        if (!string.IsNullOrWhiteSpace(requestedKey)
            && await catalogRepository.KeyExistsAsync(baseKey, excludeId, cancellationToken))
        {
            throw new ArgumentException("Já existe um benefício no catálogo com esta chave.");
        }

        var key = baseKey;
        var suffix = 2;
        while (await catalogRepository.KeyExistsAsync(key, excludeId, cancellationToken))
        {
            key = $"{baseKey}-{suffix}";
            suffix++;
        }

        return key;
    }

    private static BenefitCatalog MapToEntity(
        UpsertBenefitCatalogRequest request,
        Guid id,
        DateTimeOffset now,
        string catalogKey) =>
        new()
        {
            Id = id,
            CatalogKey = catalogKey,
            Title = request.Title.Trim(),
            Desc = request.Desc.Trim(),
            Category = request.Category.Trim(),
            Provider = request.Provider.Trim(),
            Status = request.Status.Trim(),
            Featured = request.Featured,
            IsActive = request.IsActive,
            PortalUrl = NormalizeOptional(request.PortalUrl),
            HelpText = request.HelpText.Trim(),
            DefaultMonthlyValue = request.DefaultMonthlyValue,
            SortOrder = request.SortOrder,
            DefaultDetailsJson = SerializeDefaultDetails(request),
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static void ApplyCatalogRequest(BenefitCatalog entity, UpsertBenefitCatalogRequest request)
    {
        entity.Title = request.Title.Trim();
        entity.Desc = request.Desc.Trim();
        entity.Category = request.Category.Trim();
        entity.Provider = request.Provider.Trim();
        entity.Status = request.Status.Trim();
        entity.Featured = request.Featured;
        entity.IsActive = request.IsActive;
        entity.PortalUrl = NormalizeOptional(request.PortalUrl);
        entity.HelpText = request.HelpText.Trim();
        entity.DefaultMonthlyValue = request.DefaultMonthlyValue;
        entity.SortOrder = request.SortOrder;
        entity.DefaultDetailsJson = SerializeDefaultDetails(request);
    }

    private static string SerializeDefaultDetails(UpsertBenefitCatalogRequest request) =>
        BenefitDetailsJsonHelper.Serialize(request.Lines, request.Dependents, request.Notes);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BenefitCatalogItemDto Map(BenefitCatalog entity)
    {
        var (lines, dependents, notes) = BenefitDetailsJsonHelper.Deserialize(entity.DefaultDetailsJson);
        return new(
            entity.Id,
            entity.CatalogKey,
            entity.Title,
            entity.Desc,
            entity.Category,
            entity.Provider,
            entity.Status,
            entity.Featured,
            entity.IsActive,
            entity.PortalUrl,
            entity.HelpText,
            entity.DefaultMonthlyValue,
            entity.SortOrder,
            lines,
            dependents,
            notes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
