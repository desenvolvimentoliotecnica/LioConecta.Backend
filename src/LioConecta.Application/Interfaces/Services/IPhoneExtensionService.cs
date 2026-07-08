using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPhoneExtensionService
{
    Task<PhoneExtensionsBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default);
    Task<PhoneExtensionManagePolicyDto> GetManagePolicyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhoneExtensionDto>> ListAsync(string? q = null, string? department = null, bool? personLinked = null, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<PhoneExtensionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PhoneExtensionDto> CreateAsync(UpsertPhoneExtensionRequest request, CancellationToken cancellationToken = default);
    Task<PhoneExtensionDto> UpdateAsync(Guid id, UpsertPhoneExtensionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}