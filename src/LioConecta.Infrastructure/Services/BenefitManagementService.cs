using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class BenefitManagementService(
    AppDbContext db,
    IBenefitRepository benefitRepository,
    IBenefitCatalogRepository catalogRepository,
    IPersonRepository personRepository,
    IPersonService personService,
    ICurrentUserService currentUserService,
    IAppSettingsProvider settingsProvider) : IBenefitManagementService
{
    public async Task<BenefitsBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var canManage = await BenefitManageAuthorization.CanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);
        var directory = await personService.GetDirectoryAsync(null, null, cancellationToken);
        var departments = directory.Departments
            .Select(dept => new BenefitDepartmentOptionDto(dept.Id, dept.Name, dept.Count))
            .ToList();
        var catalogCount = await db.BenefitCatalogs.CountAsync(c => c.IsActive, cancellationToken);

        return new BenefitsBootstrapDto(
            canManage,
            BenefitManageAuthorization.Categories,
            BenefitManageAuthorization.Statuses,
            departments,
            catalogCount);
    }

    public async Task<BenefitManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default) =>
        new(await BenefitManageAuthorization.CanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken));

    public async Task<IReadOnlyList<BenefitManagementListItemDto>> ListManagementAsync(
        Guid? personId,
        string? departmentId,
        string? catalogKey,
        string? q,
        string? category,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);

        var items = await benefitRepository.ListForManagementAsync(
            personId, departmentId, catalogKey, q, category, includeInactive, cancellationToken);

        return items.Select(MapListItem).ToList();
    }

    public async Task<BenefitEmployeeDetailDto?> GetManagementDetailAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);

        var entity = await benefitRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapDetail(entity);
    }

    public async Task<BenefitEmployeeDetailDto> CreateAsync(
        UpsertEmployeeBenefitRequest request,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);
        ValidateEmployeeBenefitRequest(request);
        await EnsurePersonExistsAsync(request.PersonId, cancellationToken);

        if (await benefitRepository.GetByKeyIncludingInactiveAsync(
                request.PersonId, NormalizeKey(request.BenefitKey), cancellationToken) is not null)
        {
            throw new ArgumentException("Esta pessoa já possui um vínculo com esta chave de benefício.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = MapToEntity(request, Guid.NewGuid(), now);
        await benefitRepository.AddAsync(entity, cancellationToken);
        await benefitRepository.SaveChangesAsync(cancellationToken);

        return MapDetail((await benefitRepository.GetByIdAsync(entity.Id, cancellationToken))!);
    }

    public async Task<BenefitEmployeeDetailDto> UpdateAsync(
        Guid id,
        UpsertEmployeeBenefitRequest request,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);
        ValidateEmployeeBenefitRequest(request);

        var entity = await benefitRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Benefício {id} não encontrado.");

        ApplyEmployeeBenefitRequest(entity, request);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await benefitRepository.SaveChangesAsync(cancellationToken);

        return MapDetail((await benefitRepository.GetByIdAsync(entity.Id, cancellationToken))!);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);

        var entity = await benefitRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Benefício {id} não encontrado.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await benefitRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<BenefitEmployeeDetailDto> AssignFromCatalogAsync(
        AssignBenefitFromCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);

        var catalog = await catalogRepository.GetByKeyAsync(NormalizeKey(request.CatalogKey), cancellationToken)
            ?? throw new ArgumentException("Item de catálogo não encontrado.");

        await EnsurePersonExistsAsync(request.PersonId, cancellationToken);

        var existing = await benefitRepository.GetByKeyIncludingInactiveAsync(
            request.PersonId, catalog.CatalogKey, cancellationToken);

        if (existing is not null)
        {
            ApplyCatalogToEmployee(existing, catalog, request.Overrides);
            existing.IsActive = request.Overrides?.IsActive ?? true;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await benefitRepository.SaveChangesAsync(cancellationToken);
            return MapDetail((await benefitRepository.GetByIdAsync(existing.Id, cancellationToken))!);
        }

        var now = DateTimeOffset.UtcNow;
        var entity = CreateEmployeeFromCatalog(catalog, request.PersonId, request.Overrides, now);
        await benefitRepository.AddAsync(entity, cancellationToken);
        await benefitRepository.SaveChangesAsync(cancellationToken);
        return MapDetail((await benefitRepository.GetByIdAsync(entity.Id, cancellationToken))!);
    }

    public async Task<BulkBenefitOperationResultDto> BulkAssignFromCatalogAsync(
        BulkAssignBenefitsRequest request,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);

        var catalog = await catalogRepository.GetByKeyAsync(NormalizeKey(request.CatalogKey), cancellationToken)
            ?? throw new ArgumentException("Item de catálogo não encontrado.");

        var people = await ResolveTargetsAsync(request.Target, cancellationToken);
        if (people.Count == 0)
        {
            throw new ArgumentException("Nenhuma pessoa elegível encontrada para a operação.");
        }

        var updateExisting = string.Equals(request.OnDuplicate, "update", StringComparison.OrdinalIgnoreCase);
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<BulkBenefitOperationErrorDto>();

        foreach (var person in people)
        {
            try
            {
                var existing = await benefitRepository.GetByKeyIncludingInactiveAsync(
                    person.Id, catalog.CatalogKey, cancellationToken);

                if (existing is not null)
                {
                    if (!updateExisting)
                    {
                        skipped++;
                        continue;
                    }

                    ApplyCatalogToEmployee(existing, catalog, request.Overrides);
                    existing.IsActive = request.Overrides?.IsActive ?? true;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    updated++;
                }
                else
                {
                    var entity = CreateEmployeeFromCatalog(
                        catalog, person.Id, request.Overrides, DateTimeOffset.UtcNow);
                    await benefitRepository.AddAsync(entity, cancellationToken);
                    created++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new BulkBenefitOperationErrorDto(person.Id, ex.Message));
            }
        }

        await benefitRepository.SaveChangesAsync(cancellationToken);
        return new BulkBenefitOperationResultDto(created, updated, skipped, errors.Count, errors);
    }

    public async Task<BulkBenefitOperationResultDto> BulkSetActiveAsync(
        BulkSetActiveBenefitsRequest request,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);

        var people = await ResolveTargetsAsync(request.Target, cancellationToken);
        if (people.Count == 0)
        {
            throw new ArgumentException("Nenhuma pessoa elegível encontrada para a operação.");
        }

        var catalogKey = string.IsNullOrWhiteSpace(request.CatalogKey)
            ? null
            : NormalizeKey(request.CatalogKey);

        var updated = 0;
        var skipped = 0;
        var errors = new List<BulkBenefitOperationErrorDto>();

        foreach (var person in people)
        {
            try
            {
                var benefits = await db.EmployeeBenefits
                    .Where(b => b.PersonId == person.Id)
                    .Where(b => catalogKey == null || b.BenefitKey == catalogKey)
                    .ToListAsync(cancellationToken);

                if (benefits.Count == 0)
                {
                    skipped++;
                    continue;
                }

                var touched = false;
                foreach (var benefit in benefits)
                {
                    if (request.IsActive)
                    {
                        if (!benefit.IsActive)
                        {
                            benefit.IsActive = true;
                            benefit.UpdatedAt = DateTimeOffset.UtcNow;
                            touched = true;
                        }
                    }
                    else if (benefit.IsActive)
                    {
                        benefit.IsActive = false;
                        benefit.UpdatedAt = DateTimeOffset.UtcNow;
                        touched = true;
                    }
                }

                if (touched)
                {
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new BulkBenefitOperationErrorDto(person.Id, ex.Message));
            }
        }

        await benefitRepository.SaveChangesAsync(cancellationToken);
        return new BulkBenefitOperationResultDto(0, updated, skipped, errors.Count, errors);
    }

    public async Task<BulkBenefitPreviewDto> BulkPreviewAsync(
        string operation,
        BulkBenefitTargetRequest target,
        string? catalogKey,
        string? onDuplicate,
        CancellationToken cancellationToken = default)
    {
        await BenefitManageAuthorization.EnsureCanManageAsync(
            db, currentUserService, settingsProvider, cancellationToken);

        var people = await ResolveTargetsAsync(target, cancellationToken);
        if (people.Count == 0)
        {
            return new BulkBenefitPreviewDto(0, 0, 0, 0, 0, []);
        }

        var normalizedOp = operation.Trim().ToLowerInvariant();
        var normalizedKey = string.IsNullOrWhiteSpace(catalogKey) ? null : NormalizeKey(catalogKey);
        var updateExisting = string.Equals(onDuplicate, "update", StringComparison.OrdinalIgnoreCase);

        var wouldCreate = 0;
        var wouldUpdate = 0;
        var wouldSkip = 0;
        var matchingBenefits = 0;

        foreach (var person in people)
        {
            var links = await db.EmployeeBenefits
                .AsNoTracking()
                .Where(b => b.PersonId == person.Id)
                .Where(b => normalizedKey == null || b.BenefitKey == normalizedKey)
                .ToListAsync(cancellationToken);

            matchingBenefits += links.Count;

            switch (normalizedOp)
            {
                case "assign":
                    if (normalizedKey is null)
                    {
                        wouldSkip++;
                        break;
                    }

                    var existing = links.FirstOrDefault(b => b.BenefitKey == normalizedKey);
                    if (existing is null)
                    {
                        wouldCreate++;
                    }
                    else if (updateExisting)
                    {
                        wouldUpdate++;
                    }
                    else
                    {
                        wouldSkip++;
                    }

                    break;

                case "deactivate":
                    if (links.Any(b => b.IsActive))
                    {
                        wouldUpdate++;
                    }
                    else
                    {
                        wouldSkip++;
                    }

                    break;

                case "activate":
                    if (links.Any(b => !b.IsActive))
                    {
                        wouldUpdate++;
                    }
                    else if (links.Count == 0)
                    {
                        wouldSkip++;
                    }
                    else
                    {
                        wouldSkip++;
                    }

                    break;

                default:
                    throw new ArgumentException("Operação de preview inválida.");
            }
        }

        var sample = people
            .Take(5)
            .Select(person => new BulkBenefitPreviewPersonDto(person.Id, person.Name))
            .ToList();

        return new BulkBenefitPreviewDto(
            people.Count,
            matchingBenefits,
            wouldCreate,
            wouldUpdate,
            wouldSkip,
            sample);
    }

    private async Task<IReadOnlyList<Person>> ResolveTargetsAsync(
        BulkBenefitTargetRequest target,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();

        foreach (var personId in target.PersonIds ?? [])
        {
            ids.Add(personId);
        }

        foreach (var departmentId in target.DepartmentIds ?? [])
        {
            if (string.IsNullOrWhiteSpace(departmentId))
            {
                continue;
            }

            var people = await personRepository.GetDirectoryPeopleAsync(null, departmentId.Trim(), cancellationToken);
            foreach (var person in people)
            {
                ids.Add(person.Id);
            }
        }

        foreach (var excluded in target.ExcludePersonIds ?? [])
        {
            ids.Remove(excluded);
        }

        if (ids.Count == 0)
        {
            return [];
        }

        return await db.People
            .AsNoTracking()
            .Where(person => ids.Contains(person.Id) && person.IsActive)
            .OrderBy(person => person.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task EnsurePersonExistsAsync(Guid personId, CancellationToken cancellationToken)
    {
        var exists = await db.People.AsNoTracking().AnyAsync(p => p.Id == personId && p.IsActive, cancellationToken);
        if (!exists)
        {
            throw new ArgumentException("Pessoa não encontrada ou inativa.");
        }
    }

    private static void ValidateEmployeeBenefitRequest(UpsertEmployeeBenefitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BenefitKey))
        {
            throw new ArgumentException("Chave do benefício é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Título é obrigatório.");
        }
    }

    private static string NormalizeKey(string key) => key.Trim().ToLowerInvariant();

    private static string BuildDetailsJsonFromCatalog(
        BenefitCatalog catalog,
        BenefitAssignmentOverridesDto? overrides)
    {
        var defaults = BenefitDetailsJsonHelper.Deserialize(catalog.DefaultDetailsJson);
        return BenefitDetailsJsonHelper.Serialize(
            overrides?.Lines ?? defaults.Lines,
            overrides?.Dependents ?? defaults.Dependents,
            overrides?.Notes ?? defaults.Notes);
    }

    private static EmployeeBenefit CreateEmployeeFromCatalog(
        BenefitCatalog catalog,
        Guid personId,
        BenefitAssignmentOverridesDto? overrides,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            BenefitKey = catalog.CatalogKey,
            Title = catalog.Title,
            Desc = catalog.Desc,
            Category = catalog.Category,
            Provider = catalog.Provider,
            Status = catalog.Status,
            Featured = catalog.Featured,
            IsActive = overrides?.IsActive ?? true,
            PortalUrl = catalog.PortalUrl,
            HelpText = catalog.HelpText,
            MonthlyValue = overrides?.MonthlyValue ?? catalog.DefaultMonthlyValue,
            DetailsJson = BuildDetailsJsonFromCatalog(catalog, overrides),
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static void ApplyCatalogToEmployee(
        EmployeeBenefit entity,
        BenefitCatalog catalog,
        BenefitAssignmentOverridesDto? overrides)
    {
        entity.Title = catalog.Title;
        entity.Desc = catalog.Desc;
        entity.Category = catalog.Category;
        entity.Provider = catalog.Provider;
        entity.Status = catalog.Status;
        entity.Featured = catalog.Featured;
        entity.PortalUrl = catalog.PortalUrl;
        entity.HelpText = catalog.HelpText;

        if (overrides?.MonthlyValue is not null)
        {
            entity.MonthlyValue = overrides.MonthlyValue;
        }
        else if (entity.MonthlyValue is null)
        {
            entity.MonthlyValue = catalog.DefaultMonthlyValue;
        }

        if (overrides?.Lines is not null || overrides?.Dependents is not null || overrides?.Notes is not null)
        {
            var current = BenefitDetailsJsonHelper.Deserialize(entity.DetailsJson);
            entity.DetailsJson = BenefitDetailsJsonHelper.Serialize(
                overrides.Lines ?? current.Lines,
                overrides.Dependents ?? current.Dependents,
                overrides.Notes ?? current.Notes);
        }
    }

    private static EmployeeBenefit MapToEntity(UpsertEmployeeBenefitRequest request, Guid id, DateTimeOffset now) =>
        new()
        {
            Id = id,
            PersonId = request.PersonId,
            BenefitKey = NormalizeKey(request.BenefitKey),
            Title = request.Title.Trim(),
            Desc = request.Desc.Trim(),
            Category = request.Category.Trim(),
            Provider = request.Provider.Trim(),
            Status = request.Status.Trim(),
            Featured = request.Featured,
            IsActive = request.IsActive,
            PortalUrl = NormalizeOptional(request.PortalUrl),
            HelpText = request.HelpText.Trim(),
            MonthlyValue = request.MonthlyValue,
            DetailsJson = BenefitDetailsJsonHelper.Serialize(request.Lines, request.Dependents, request.Notes),
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static void ApplyEmployeeBenefitRequest(EmployeeBenefit entity, UpsertEmployeeBenefitRequest request)
    {
        entity.PersonId = request.PersonId;
        entity.BenefitKey = NormalizeKey(request.BenefitKey);
        entity.Title = request.Title.Trim();
        entity.Desc = request.Desc.Trim();
        entity.Category = request.Category.Trim();
        entity.Provider = request.Provider.Trim();
        entity.Status = request.Status.Trim();
        entity.Featured = request.Featured;
        entity.IsActive = request.IsActive;
        entity.PortalUrl = NormalizeOptional(request.PortalUrl);
        entity.HelpText = request.HelpText.Trim();
        entity.MonthlyValue = request.MonthlyValue;
        entity.DetailsJson = BenefitDetailsJsonHelper.Serialize(request.Lines, request.Dependents, request.Notes);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BenefitManagementListItemDto MapListItem(EmployeeBenefit entity) =>
        new(
            entity.Id,
            entity.PersonId,
            entity.Person?.Name ?? "—",
            PersonDepartmentHelper.GetName(entity.Person),
            entity.BenefitKey,
            entity.Title,
            entity.Category,
            entity.Provider,
            entity.Status,
            entity.IsActive,
            entity.MonthlyValue,
            entity.UpdatedAt);

    private static BenefitEmployeeDetailDto MapDetail(EmployeeBenefit entity)
    {
        var details = BenefitDetailsJsonHelper.Deserialize(entity.DetailsJson);
        return new BenefitEmployeeDetailDto(
            entity.Id,
            entity.PersonId,
            entity.Person?.Name ?? "—",
            PersonDepartmentHelper.GetName(entity.Person),
            entity.BenefitKey,
            entity.Title,
            entity.Desc,
            entity.Category,
            entity.Provider,
            entity.Status,
            entity.Featured,
            entity.IsActive,
            entity.PortalUrl,
            entity.HelpText,
            entity.MonthlyValue,
            details.Lines,
            details.Dependents,
            details.Notes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
