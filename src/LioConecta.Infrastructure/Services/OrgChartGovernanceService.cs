using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class OrgChartGovernanceService(
    AppDbContext db,
    ICurrentUserService currentUserService,
    IAppSettingsProvider settingsProvider) : IOrgChartGovernanceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string DefaultRolesJson = SerializeRoles(UserRole.Admin, UserRole.HR);

    public async Task<OrgChartSettingsDto> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var entity = await EnsureSettingsAsync(cancellationToken);
        return MapSettingsToDto(entity);
    }

    public async Task<OrgChartSettingsDto> SaveSettingsAsync(
        UpsertOrgChartSettingsRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var entity = await EnsureSettingsAsync(cancellationToken);

        entity.GovernanceEnabled = request.GovernanceEnabled;
        entity.EditAllowedRolesJson = SerializeRolesFromStrings(request.EditAllowedRoles);
        entity.EditAllowedEmailsJson = SerializeEmails(request.EditAllowedEmails);
        entity.ViewFullAllowedRolesJson = SerializeRolesFromStrings(request.ViewFullAllowedRoles);
        entity.AllowDisplayNameEdit = request.AllowDisplayNameEdit;
        entity.AllowReimport = request.AllowReimport;
        entity.ShowOverrideBadge = request.ShowOverrideBadge;
        entity.UpdatedById = updatedById;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return MapSettingsToDto(entity);
    }

    public async Task<OrgChartPolicyDto> GetPolicyAsync(CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        var email = person?.Email?.Trim().ToLowerInvariant();

        var editRoles = DeserializeRoles(settings.EditAllowedRolesJson);
        var viewFullRoles = DeserializeRoles(settings.ViewFullAllowedRolesJson);
        var editEmails = DeserializeEmails(settings.EditAllowedEmailsJson);

        var canEdit = settings.GovernanceEnabled &&
            (roles.Any(r => editRoles.Contains(r)) ||
             (email is not null && editEmails.Contains(email)));

        var canViewFull = canEdit ||
            roles.Any(r => viewFullRoles.Contains(r));

        var canImport = settings.GovernanceEnabled &&
            settings.AllowReimport &&
            (canEdit || roles.Contains(UserRole.Admin));

        var allowedFields = BuildAllowedFields(settings);

        return new OrgChartPolicyDto(
            canEdit,
            canImport,
            canEdit,
            canViewFull,
            allowedFields,
            settings.GovernanceEnabled);
    }

    public async Task<GovernedOrgChartDto> GetChartAsync(CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        var policy = await GetPolicyAsync(cancellationToken);

        var positions = await db.OrgPositions
            .AsNoTracking()
            .Include(p => p.Person)
            .Include(p => p.OrgDepartment)
            .Include(p => p.ManagerPosition)
                .ThenInclude(m => m!.Person)
            .Where(p => p.IsVisible && p.Person.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Person.Name)
            .ToListAsync(cancellationToken);

        if (!settings.GovernanceEnabled && positions.Count == 0)
        {
            return EmptyChart();
        }

        var chartPositions = positions.Where(p => p.ManagerPositionId is not null).ToList();
        var idSet = chartPositions.Select(p => p.PersonId).ToHashSet();

        var nodes = chartPositions
            .Select(p =>
            {
                var managerPersonId = p.ManagerPosition?.PersonId;
                var isOrphan = managerPersonId is not null && !idSet.Contains(managerPersonId.Value);
                return ToGovernedNode(p, isOrphan);
            })
            .ToList();

        var unassignedPositions = positions
            .Where(p => p.ManagerPositionId is null)
            .Select(p => ToGovernedNode(p))
            .ToList();

        if (policy.CanViewFull)
        {
            var positionedPersonIds = positions.Select(p => p.PersonId).ToHashSet();
            var unassignedPeople = await db.People
                .AsNoTracking()
                .Where(p => p.IsActive && !positionedPersonIds.Contains(p.Id))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            unassignedPositions = unassignedPositions
                .Concat(unassignedPeople.Select(ToUnassignedPersonNode))
                .ToList();
        }

        var rootIds = chartPositions
            .Where(p =>
            {
                var managerPersonId = p.ManagerPosition?.PersonId;
                return managerPersonId is null || !idSet.Contains(managerPersonId.Value);
            })
            .Select(p => p.PersonId)
            .ToList();

        DateTimeOffset? syncedAt = null;
        var syncedRaw = settingsProvider.GetString(AppSettingKeys.GraphDirectoryLastSyncUtc);
        if (DateTimeOffset.TryParse(syncedRaw, out var parsed))
        {
            syncedAt = parsed;
        }

        return new GovernedOrgChartDto(
            nodes,
            rootIds.FirstOrDefault(),
            positions.Count,
            rootIds,
            nodes.Count(n => n.IsOrphan),
            syncedAt,
            unassignedPositions,
            unassignedPositions.Count);
    }

    public async Task<OrgChartGovernanceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);

        var totalPositions = await db.OrgPositions.CountAsync(cancellationToken);
        var visiblePositions = await db.OrgPositions.CountAsync(p => p.IsVisible, cancellationToken);
        var manualOverrides = await db.OrgPositions.CountAsync(p => p.HasManualOverride, cancellationToken);
        var totalDepartments = await db.OrgDepartments.CountAsync(cancellationToken);
        var activeDepartments = await db.OrgDepartments.CountAsync(d => d.IsActive, cancellationToken);

        return new OrgChartGovernanceSummaryDto(
            totalPositions,
            visiblePositions,
            manualOverrides,
            totalDepartments,
            activeDepartments,
            settings.LastImportAt);
    }

    public async Task<IReadOnlyList<OrgPositionDetailDto>> ListPositionsAsync(CancellationToken cancellationToken)
    {
        var positions = await db.OrgPositions
            .AsNoTracking()
            .Include(p => p.Person)
            .Include(p => p.OrgDepartment)
            .Include(p => p.ManagerPosition)
                .ThenInclude(m => m!.Person)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Person.Name)
            .ToListAsync(cancellationToken);

        return positions.Select(MapPositionToDto).ToList();
    }

    public async Task<OrgPositionDetailDto?> GetPositionAsync(Guid id, CancellationToken cancellationToken)
    {
        var position = await db.OrgPositions
            .AsNoTracking()
            .Include(p => p.Person)
            .Include(p => p.OrgDepartment)
            .Include(p => p.ManagerPosition)
                .ThenInclude(m => m!.Person)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return position is null ? null : MapPositionToDto(position);
    }

    public async Task<OrgPositionDetailDto> UpdatePositionAsync(
        Guid id,
        UpsertOrgPositionRequest request,
        CancellationToken cancellationToken)
    {
        var position = await db.OrgPositions
            .Include(p => p.Person)
            .Include(p => p.OrgDepartment)
            .Include(p => p.ManagerPosition)
                .ThenInclude(m => m!.Person)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Position was not found.");

        if (request.ManagerPositionId == id)
        {
            throw new InvalidOperationException("A position cannot be its own manager.");
        }

        var managerMap = await BuildManagerMapAsync(cancellationToken);
        ValidateNoManagerCycle(id, request.ManagerPositionId, managerMap);

        if (request.Title is not null)
        {
            position.Title = NormalizeOptional(request.Title);
        }

        if (request.DepartmentName is not null)
        {
            position.DepartmentName = NormalizeOptional(request.DepartmentName);
        }

        if (request.OrgDepartmentId.HasValue)
        {
            var orgDepartment = await db.OrgDepartments
                .FirstOrDefaultAsync(d => d.Id == request.OrgDepartmentId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Department was not found.");

            position.OrgDepartmentId = orgDepartment.Id;
            position.DepartmentName = orgDepartment.Name;
        }

        position.ManagerPositionId = request.ManagerPositionId;

        if (request.IsVisible.HasValue)
        {
            position.IsVisible = request.IsVisible.Value;
        }

        if (request.SortOrder.HasValue)
        {
            position.SortOrder = request.SortOrder.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            var settings = await EnsureSettingsAsync(cancellationToken);
            if (!settings.AllowDisplayNameEdit)
            {
                throw new InvalidOperationException("Display name editing is not allowed.");
            }

            position.Person.Name = request.DisplayName.Trim();
            position.Person.UpdatedAt = DateTimeOffset.UtcNow;
        }

        position.HasManualOverride = true;
        position.Source = OrgPositionSource.Manual;
        position.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(position).Reference(p => p.ManagerPosition).LoadAsync(cancellationToken);
        if (position.ManagerPosition is not null)
        {
            await db.Entry(position.ManagerPosition).Reference(m => m.Person).LoadAsync(cancellationToken);
        }

        return MapPositionToDto(position);
    }

    public async Task<OrgPositionDetailDto> CreatePositionAsync(
        CreateOrgPositionRequest request,
        CancellationToken cancellationToken)
    {
        var person = await db.People.FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken)
            ?? throw new InvalidOperationException("Person was not found.");

        var existing = await db.OrgPositions.AnyAsync(p => p.PersonId == request.PersonId, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException("A position already exists for this person.");
        }

        if (request.ManagerPositionId.HasValue)
        {
            var managerMap = await BuildManagerMapAsync(cancellationToken);
            var tempId = Guid.NewGuid();
            ValidateNoManagerCycle(tempId, request.ManagerPositionId, managerMap);
        }

        var now = DateTimeOffset.UtcNow;
        string? departmentName = NormalizeOptional(request.DepartmentName) ?? PersonDepartmentHelper.GetName(person);
        if (request.OrgDepartmentId.HasValue)
        {
            var orgDepartment = await db.OrgDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.OrgDepartmentId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Department was not found.");
            departmentName = orgDepartment.Name;
        }

        var position = new OrgPosition
        {
            Id = Guid.NewGuid(),
            PersonId = request.PersonId,
            Title = NormalizeOptional(request.Title) ?? person.Title,
            DepartmentName = departmentName,
            OrgDepartmentId = request.OrgDepartmentId,
            ManagerPositionId = request.ManagerPositionId,
            IsVisible = request.IsVisible,
            SortOrder = request.SortOrder,
            HasManualOverride = true,
            Source = OrgPositionSource.Manual,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.OrgPositions.Add(position);
        await db.SaveChangesAsync(cancellationToken);

        position.Person = person;
        if (request.ManagerPositionId.HasValue)
        {
            position.ManagerPosition = await db.OrgPositions
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == request.ManagerPositionId.Value, cancellationToken);
        }

        return MapPositionToDto(position);
    }

    public async Task DeletePositionAsync(Guid id, CancellationToken cancellationToken)
    {
        var position = await db.OrgPositions
            .Include(p => p.DirectReports)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Position was not found.");

        foreach (var report in position.DirectReports)
        {
            report.ManagerPositionId = null;
            report.UpdatedAt = DateTimeOffset.UtcNow;
        }

        db.OrgPositions.Remove(position);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrgDepartmentDto>> ListDepartmentsAsync(CancellationToken cancellationToken)
    {
        var departments = await db.OrgDepartments
            .AsNoTracking()
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(MapDepartmentToDto).ToList();
    }

    public async Task<OrgDepartmentDto> CreateDepartmentAsync(
        UpsertOrgDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateDepartmentParentAsync(null, request.ParentDepartmentId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var department = new OrgDepartment
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(request.Name),
            ParentDepartmentId = request.ParentDepartmentId,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.OrgDepartments.Add(department);
        await db.SaveChangesAsync(cancellationToken);
        return MapDepartmentToDto(department);
    }

    public async Task<OrgDepartmentDto> UpdateDepartmentAsync(
        Guid id,
        UpsertOrgDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var department = await db.OrgDepartments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Department was not found.");

        await ValidateDepartmentParentAsync(id, request.ParentDepartmentId, cancellationToken);

        department.Name = NormalizeRequired(request.Name);
        department.ParentDepartmentId = request.ParentDepartmentId;
        department.SortOrder = request.SortOrder;
        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return MapDepartmentToDto(department);
    }

    public async Task DeleteDepartmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await db.OrgDepartments
            .Include(d => d.ChildDepartments)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Department was not found.");

        if (department.ChildDepartments.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete a department that has child departments.");
        }

        var inUse = await db.OrgPositions.AnyAsync(p => p.OrgDepartmentId == id, cancellationToken);
        if (inUse)
        {
            throw new InvalidOperationException("Cannot delete a department that is assigned to positions.");
        }

        db.OrgDepartments.Remove(department);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrgDepartmentMappingDto>> ListDepartmentMappingsAsync(CancellationToken cancellationToken)
    {
        var mappings = await db.OrgDepartmentMappings
            .AsNoTracking()
            .Include(m => m.OrgDepartment)
            .OrderBy(m => m.SourceName)
            .ToListAsync(cancellationToken);

        var employeeCounts = await GetDirectoryDepartmentCountsAsync(cancellationToken);

        return mappings
            .Select(m => MapMappingToDto(m, employeeCounts.GetValueOrDefault(NormalizeSourceName(m.SourceName))))
            .ToList();
    }

    public async Task<OrgDepartmentMappingDto> UpdateDepartmentMappingAsync(
        Guid id,
        UpsertOrgDepartmentMappingRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await db.OrgDepartmentMappings
            .Include(m => m.OrgDepartment)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Department mapping was not found.");

        if (request.UpdateOrgDepartmentId)
        {
            if (request.OrgDepartmentId.HasValue)
            {
                var departmentExists = await db.OrgDepartments
                    .AnyAsync(d => d.Id == request.OrgDepartmentId.Value, cancellationToken);
                if (!departmentExists)
                {
                    throw new InvalidOperationException("Target department was not found.");
                }
            }

            mapping.OrgDepartmentId = request.OrgDepartmentId;
        }

        if (request.IsActive.HasValue)
        {
            mapping.IsActive = request.IsActive.Value;
        }

        mapping.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(mapping).Reference(m => m.OrgDepartment).LoadAsync(cancellationToken);
        var employeeCounts = await GetDirectoryDepartmentCountsAsync(cancellationToken);
        return MapMappingToDto(mapping, employeeCounts.GetValueOrDefault(NormalizeSourceName(mapping.SourceName)));
    }

    public async Task<ImportDepartmentsFromDirectoryResultDto> ImportDepartmentsFromDirectoryAsync(
        ImportDepartmentsFromDirectoryRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        var directoryCounts = await GetDirectoryDepartmentCountsAsync(cancellationToken);
        var existingMappings = await db.OrgDepartmentMappings.ToListAsync(cancellationToken);
        var mappingsBySource = existingMappings.ToDictionary(
            m => NormalizeSourceName(m.SourceName),
            StringComparer.OrdinalIgnoreCase);

        var departments = await db.OrgDepartments.ToListAsync(cancellationToken);
        var departmentsByName = departments.ToDictionary(
            d => NormalizeSourceName(d.Name),
            StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var mappingsImported = 0;
        var departmentsCreated = 0;
        var departmentsLinked = 0;

        foreach (var (sourceName, employeeCount) in directoryCounts)
        {
            _ = employeeCount;

            if (!mappingsBySource.TryGetValue(sourceName, out var mapping))
            {
                mapping = new OrgDepartmentMapping
                {
                    Id = Guid.NewGuid(),
                    SourceName = sourceName,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.OrgDepartmentMappings.Add(mapping);
                mappingsBySource[sourceName] = mapping;
                mappingsImported++;
            }
            else
            {
                mapping.SourceName = sourceName;
                mapping.UpdatedAt = now;
            }

            if (mapping.OrgDepartmentId is not null || !request.CreateMissingDepartments)
            {
                continue;
            }

            if (!departmentsByName.TryGetValue(sourceName, out var department))
            {
                department = new OrgDepartment
                {
                    Id = Guid.NewGuid(),
                    Name = sourceName,
                    SortOrder = departments.Count + departmentsCreated,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.OrgDepartments.Add(department);
                departments.Add(department);
                departmentsByName[sourceName] = department;
                departmentsCreated++;
            }

            mapping.OrgDepartmentId = department.Id;
            mapping.UpdatedAt = now;
            departmentsLinked++;
        }

        settings.UpdatedById = updatedById;
        settings.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        await ApplyDepartmentMappingsToPositionsAsync(force: false, cancellationToken);

        var unmappedCount = await db.OrgDepartmentMappings
            .CountAsync(m => m.IsActive && m.OrgDepartmentId == null, cancellationToken);

        return new ImportDepartmentsFromDirectoryResultDto(
            mappingsImported,
            departmentsCreated,
            departmentsLinked,
            unmappedCount);
    }

    public async Task<OrgChartGovernanceSummaryDto> ImportFromGraphAsync(
        ImportFromGraphRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);
        var people = await db.People
            .Include(p => p.Manager)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var positions = await db.OrgPositions.ToListAsync(cancellationToken);
        var positionsByPersonId = positions.ToDictionary(p => p.PersonId);
        var now = DateTimeOffset.UtcNow;

        foreach (var person in people)
        {
            if (!positionsByPersonId.TryGetValue(person.Id, out var position))
            {
                position = new OrgPosition
                {
                    Id = Guid.NewGuid(),
                    PersonId = person.Id,
                    Title = person.Title,
                    DepartmentName = PersonDepartmentHelper.GetName(person),
                    IsVisible = true,
                    SortOrder = 0,
                    HasManualOverride = false,
                    Source = OrgPositionSource.Graph,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                db.OrgPositions.Add(position);
                positionsByPersonId[person.Id] = position;
                positions.Add(position);
                continue;
            }

            if (position.HasManualOverride && !request.Force)
            {
                continue;
            }

            position.Title = person.Title;
            position.DepartmentName = PersonDepartmentHelper.GetName(person);
            position.HasManualOverride = false;
            position.Source = OrgPositionSource.Graph;
            position.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var person in people)
        {
            if (!positionsByPersonId.TryGetValue(person.Id, out var position))
            {
                continue;
            }

            if (position.HasManualOverride && !request.Force)
            {
                continue;
            }

            Guid? managerPositionId = null;
            if (person.ManagerId is not null &&
                positionsByPersonId.TryGetValue(person.ManagerId.Value, out var managerPosition))
            {
                managerPositionId = managerPosition.Id;
            }

            if (position.ManagerPositionId != managerPositionId)
            {
                position.ManagerPositionId = managerPositionId;
                position.UpdatedAt = now;
            }
        }

        settings.LastImportAt = now;
        settings.UpdatedById = updatedById;
        settings.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        await ApplyDepartmentMappingsToPositionsAsync(request.Force, cancellationToken);
        return await GetSummaryAsync(cancellationToken);
    }

    private async Task<OrgChartSettings> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        var entity = await db.OrgChartSettings.FirstOrDefaultAsync(cancellationToken);
        if (entity is not null)
        {
            return entity;
        }

        var now = DateTimeOffset.UtcNow;
        entity = new OrgChartSettings
        {
            Id = Guid.NewGuid(),
            GovernanceEnabled = false,
            EditAllowedRolesJson = DefaultRolesJson,
            EditAllowedEmailsJson = "[]",
            ViewFullAllowedRolesJson = DefaultRolesJson,
            AllowDisplayNameEdit = false,
            AllowReimport = true,
            ShowOverrideBadge = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.OrgChartSettings.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static GovernedOrgChartDto EmptyChart()
        => new([], null, 0, [], 0, null, [], 0);

    private static GovernedOrgChartNodeDto ToGovernedNode(OrgPosition position, bool isOrphan = false)
    {
        var person = position.Person;
        var graphManagerName = person.Manager?.Name;

        return new GovernedOrgChartNodeDto(
            person.Id,
            person.OrgChartId,
            person.Slug,
            person.Name,
            position.Title ?? person.Title,
            ResolvePhotoUrl(person),
            ResolveGovernedDepartmentName(position),
            position.ManagerPosition?.PersonId,
            JsonMapper.DeserializeStringList(person.TagsJson),
            isOrphan,
            person.Email,
            person.TeamsUpn,
            person.Phone,
            person.Location,
            person.HireDate,
            position.Id,
            position.HasManualOverride,
            person.Title,
            PersonDepartmentHelper.GetName(person),
            graphManagerName,
            position.OrgDepartmentId,
            position.ManagerPositionId,
            position.ManagerPosition?.Person?.Name,
            position.IsVisible);
    }

    private static GovernedOrgChartNodeDto ToUnassignedPersonNode(Person person)
        => new(
            person.Id,
            person.OrgChartId,
            person.Slug,
            person.Name,
            person.Title,
            ResolvePhotoUrl(person),
            PersonDepartmentHelper.GetName(person),
            null,
            JsonMapper.DeserializeStringList(person.TagsJson),
            false,
            person.Email,
            person.TeamsUpn,
            person.Phone,
            person.Location,
            person.HireDate,
            Guid.Empty,
            false,
            person.Title,
            PersonDepartmentHelper.GetName(person),
            person.Manager?.Name,
            null,
            null,
            person.Manager?.Name,
            true);

    private static string? ResolveGovernedDepartmentName(OrgPosition position)
    {
        if (position.OrgDepartment is not null && !string.IsNullOrWhiteSpace(position.OrgDepartment.Name))
        {
            return position.OrgDepartment.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(position.DepartmentName))
        {
            return position.DepartmentName.Trim();
        }

        return PersonDepartmentHelper.GetName(position.Person);
    }

    private static OrgPositionDetailDto MapPositionToDto(OrgPosition position)
        => new(
            position.Id,
            position.PersonId,
            position.Person.Name,
            position.Title,
            ResolveGovernedDepartmentName(position),
            position.OrgDepartmentId,
            position.ManagerPositionId,
            position.ManagerPosition?.Person?.Name,
            position.IsVisible,
            position.SortOrder,
            position.HasManualOverride,
            position.Source,
            position.Person.Title,
            PersonDepartmentHelper.GetName(position.Person),
            position.UpdatedAt);

    private static OrgDepartmentDto MapDepartmentToDto(OrgDepartment department)
        => new(department.Id, department.Name, department.ParentDepartmentId, department.SortOrder, department.IsActive);

    private static OrgDepartmentMappingDto MapMappingToDto(OrgDepartmentMapping mapping, int employeeCount)
        => new(
            mapping.Id,
            mapping.SourceName,
            mapping.OrgDepartmentId,
            mapping.OrgDepartment?.Name,
            employeeCount,
            mapping.IsActive);

    private static string NormalizeSourceName(string value)
        => value.Trim();

    private async Task<Dictionary<string, int>> GetDirectoryDepartmentCountsAsync(CancellationToken cancellationToken)
    {
        var people = await db.People
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        return people
            .Select(PersonDepartmentHelper.GetName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => NormalizeSourceName(name!), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task ApplyDepartmentMappingsToPositionsAsync(bool force, CancellationToken cancellationToken)
    {
        var mappings = await db.OrgDepartmentMappings
            .AsNoTracking()
            .Where(m => m.IsActive && m.OrgDepartmentId != null)
            .ToListAsync(cancellationToken);

        if (mappings.Count == 0)
        {
            return;
        }

        var mappingBySource = mappings.ToDictionary(
            m => NormalizeSourceName(m.SourceName),
            m => m.OrgDepartmentId,
            StringComparer.OrdinalIgnoreCase);

        var positions = await db.OrgPositions
            .Include(p => p.Person)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var position in positions)
        {
            if (position.HasManualOverride && !force)
            {
                continue;
            }

            var sourceName = PersonDepartmentHelper.GetName(position.Person);
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                continue;
            }

            if (!mappingBySource.TryGetValue(NormalizeSourceName(sourceName), out var orgDepartmentId))
            {
                continue;
            }

            if (position.OrgDepartmentId != orgDepartmentId)
            {
                position.OrgDepartmentId = orgDepartmentId;
                position.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static OrgChartSettingsDto MapSettingsToDto(OrgChartSettings entity)
        => new(
            entity.GovernanceEnabled,
            DeserializeRoleStrings(entity.EditAllowedRolesJson),
            DeserializeEmails(entity.EditAllowedEmailsJson).ToList(),
            DeserializeRoleStrings(entity.ViewFullAllowedRolesJson),
            entity.AllowDisplayNameEdit,
            entity.AllowReimport,
            entity.ShowOverrideBadge,
            entity.UpdatedAt,
            entity.UpdatedById);

    private static IReadOnlyList<string> BuildAllowedFields(OrgChartSettings settings)
    {
        var fields = new List<string> { "title", "departmentName", "managerPositionId", "isVisible", "sortOrder", "orgDepartmentId" };
        if (settings.AllowDisplayNameEdit)
        {
            fields.Add("displayName");
        }

        return fields;
    }

    private async Task<Dictionary<Guid, Guid?>> BuildManagerMapAsync(CancellationToken cancellationToken)
    {
        return await db.OrgPositions
            .AsNoTracking()
            .Select(p => new { p.Id, p.ManagerPositionId })
            .ToDictionaryAsync(p => p.Id, p => p.ManagerPositionId, cancellationToken);
    }

    private static void ValidateNoManagerCycle(Guid positionId, Guid? managerPositionId, IReadOnlyDictionary<Guid, Guid?> managerMap)
    {
        if (managerPositionId is null)
        {
            return;
        }

        if (managerPositionId == positionId)
        {
            throw new InvalidOperationException("Manager assignment would create a cycle.");
        }

        var visited = new HashSet<Guid> { positionId };
        var current = managerPositionId;

        while (current is not null)
        {
            if (!visited.Add(current.Value))
            {
                throw new InvalidOperationException("Manager assignment would create a cycle.");
            }

            current = managerMap.TryGetValue(current.Value, out var next) ? next : null;
        }
    }

    private async Task ValidateDepartmentParentAsync(
        Guid? departmentId,
        Guid? parentDepartmentId,
        CancellationToken cancellationToken)
    {
        if (parentDepartmentId is null)
        {
            return;
        }

        if (departmentId.HasValue && parentDepartmentId == departmentId)
        {
            throw new InvalidOperationException("A department cannot be its own parent.");
        }

        var parentExists = await db.OrgDepartments.AnyAsync(d => d.Id == parentDepartmentId, cancellationToken);
        if (!parentExists)
        {
            throw new InvalidOperationException("Parent department was not found.");
        }

        if (!departmentId.HasValue)
        {
            return;
        }

        var current = parentDepartmentId;
        var visited = new HashSet<Guid> { departmentId.Value };
        while (current is not null)
        {
            if (!visited.Add(current.Value))
            {
                throw new InvalidOperationException("Department parent assignment would create a cycle.");
            }

            current = await db.OrgDepartments
                .AsNoTracking()
                .Where(d => d.Id == current)
                .Select(d => d.ParentDepartmentId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private static string SerializeRoles(params UserRole[] roles)
        => JsonSerializer.Serialize(roles.Select(r => r.ToString()).ToArray(), JsonOptions);

    private static string SerializeRolesFromStrings(IReadOnlyList<string> roles)
    {
        var normalized = roles
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return JsonSerializer.Serialize(normalized, JsonOptions);
    }

    private static string SerializeEmails(IReadOnlyList<string> emails)
    {
        var normalized = emails
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return JsonSerializer.Serialize(normalized, JsonOptions);
    }

    private static HashSet<UserRole> DeserializeRoles(string? json)
    {
        var roleStrings = DeserializeRoleStrings(json);
        var roles = new HashSet<UserRole>();
        foreach (var roleString in roleStrings)
        {
            if (Enum.TryParse<UserRole>(roleString, true, out var role))
            {
                roles.Add(role);
            }
        }

        return roles;
    }

    private static IReadOnlyList<string> DeserializeRoleStrings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static HashSet<string> DeserializeEmails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var emails = JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        return emails
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeRequired(string value)
        => string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("Name is required.")
            : value.Trim();

    private static string? ResolvePhotoUrl(Person person)
        => PersonPhotoResolver.ResolveEffectivePhotoUrl(person);
}
