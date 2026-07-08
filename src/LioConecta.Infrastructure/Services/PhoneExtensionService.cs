using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class PhoneExtensionService(
    AppDbContext db,
    ICurrentUserService currentUserService,
    IAppSettingsProvider settingsProvider) : IPhoneExtensionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<PhoneExtensionsBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var canManage = await CanManageAsync(cancellationToken);
        var query = db.PhoneExtensions.AsNoTracking().Where(x => x.IsActive);
        var total = await query.CountAsync(cancellationToken);
        var departments = await query.Select(x => x.Department).Where(d => d != null && d != "").Distinct().OrderBy(d => d).ToListAsync(cancellationToken);
        return new PhoneExtensionsBootstrapDto(canManage, total, departments);
    }

    public async Task<PhoneExtensionManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default)
        => new(await CanManageAsync(cancellationToken));

    public async Task<IReadOnlyList<PhoneExtensionDto>> ListAsync(string? q = null, string? department = null, bool? personLinked = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = db.PhoneExtensions.AsNoTracking().Include(x => x.Person).AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(department)) { var dept = department.Trim(); query = query.Where(x => x.Department == dept); }
        if (personLinked == true) query = query.Where(x => x.PersonId != null);
        else if (personLinked == false) query = query.Where(x => x.PersonId == null);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Extension.ToLower().Contains(term) || (x.Department != null && x.Department.ToLower().Contains(term)) || (x.Title != null && x.Title.ToLower().Contains(term)) || (x.Email != null && x.Email.ToLower().Contains(term)) || (x.ManagerName != null && x.ManagerName.ToLower().Contains(term)) || (x.Mobile != null && x.Mobile.ToLower().Contains(term)));
        }
        var items = await query.OrderBy(x => x.Department).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<PhoneExtensionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.PhoneExtensions.AsNoTracking().Include(x => x.Person).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<PhoneExtensionDto> CreateAsync(UpsertPhoneExtensionRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(cancellationToken);
        ValidateRequest(request);
        var now = DateTimeOffset.UtcNow;
        var personId = await ResolvePersonIdAsync(request.PersonId, request.Email, cancellationToken);
        var entity = new PhoneExtension { Id = Guid.NewGuid(), Name = request.Name.Trim(), Extension = request.Extension.Trim(), Mobile = NormalizeOptional(request.Mobile), Department = request.Department.Trim(), Title = NormalizeOptional(request.Title), Email = NormalizeOptionalEmail(request.Email), ManagerName = NormalizeOptional(request.ManagerName), PersonId = personId, IsActive = request.IsActive, CreatedAt = now, UpdatedAt = now };
        db.PhoneExtensions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<PhoneExtensionDto> UpdateAsync(Guid id, UpsertPhoneExtensionRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(cancellationToken);
        ValidateRequest(request);
        var entity = await db.PhoneExtensions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException($"Ramal {id} nao encontrado.");
        var personId = await ResolvePersonIdAsync(request.PersonId, request.Email, cancellationToken);
        entity.Name = request.Name.Trim(); entity.Extension = request.Extension.Trim(); entity.Mobile = NormalizeOptional(request.Mobile); entity.Department = request.Department.Trim(); entity.Title = NormalizeOptional(request.Title); entity.Email = NormalizeOptionalEmail(request.Email); entity.ManagerName = NormalizeOptional(request.ManagerName); entity.PersonId = personId; entity.IsActive = request.IsActive; entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(cancellationToken);
        var entity = await db.PhoneExtensions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException($"Ramal {id} nao encontrado.");
        db.PhoneExtensions.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid?> ResolvePersonIdAsync(Guid? requestedPersonId, string? email, CancellationToken cancellationToken)
    {
        if (requestedPersonId is Guid personId)
        {
            var exists = await db.People.AsNoTracking().AnyAsync(p => p.Id == personId, cancellationToken);
            if (!exists) throw new ArgumentException("Pessoa vinculada nao encontrada.");
            return personId;
        }
        var normalized = NormalizeOptionalEmail(email);
        if (normalized is null) return null;
        return await db.People.AsNoTracking().Where(p => p.Email.ToLower() == normalized).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> CanManageAsync(CancellationToken cancellationToken)
    {
        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        if (roles.Contains(UserRole.Admin)) return true;
        var allowedRoles = DeserializeRoles(settingsProvider.GetString(AppSettingKeys.RamaisAllowedRoles));
        if (roles.Any(role => allowedRoles.Contains(role))) return true;
        var allowedEmails = settingsProvider.GetStringArray(AppSettingKeys.RamaisAllowedEmails).Select(email => email.Trim().ToLowerInvariant()).Where(email => email.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (allowedEmails.Count == 0) return false;
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var email = await db.People.AsNoTracking().Where(person => person.Id == personId).Select(person => person.Email).FirstOrDefaultAsync(cancellationToken);
        return email is not null && allowedEmails.Contains(email.Trim().ToLowerInvariant());
    }

    private async Task EnsureCanManageAsync(CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken)) throw new UnauthorizedAccessException("Sem permissao para gerir a lista de ramais.");
    }

    private static void ValidateRequest(UpsertPhoneExtensionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Nome e obrigatorio.");
        if (string.IsNullOrWhiteSpace(request.Extension)) throw new ArgumentException("Ramal e obrigatorio.");
        if (string.IsNullOrWhiteSpace(request.Department)) throw new ArgumentException("Departamento e obrigatorio.");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? NormalizeOptionalEmail(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static IReadOnlyList<UserRole> DeserializeRoles(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [UserRole.HR];
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            var roles = new List<UserRole>();
            foreach (var value in values) if (Enum.TryParse<UserRole>(value, true, out var role)) roles.Add(role);
            return roles.Count > 0 ? roles : [UserRole.HR];
        }
        catch (JsonException) { return [UserRole.HR]; }
    }

    private static PhoneExtensionDto Map(PhoneExtension entity) =>
        new(entity.Id, entity.Name, entity.Extension, entity.Mobile, entity.Department, entity.Title, entity.Email, entity.ManagerName, entity.PersonId, entity.Person?.Slug, entity.Person?.Name, entity.IsActive, entity.CreatedAt, entity.UpdatedAt);
}
