using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IOrgChartGovernanceService
{
    Task<OrgChartSettingsDto> GetSettingsAsync(CancellationToken cancellationToken);

    Task<OrgChartSettingsDto> SaveSettingsAsync(
        UpsertOrgChartSettingsRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken);

    Task<OrgChartPolicyDto> GetPolicyAsync(CancellationToken cancellationToken);

    Task<GovernedOrgChartDto> GetChartAsync(CancellationToken cancellationToken);

    Task<OrgChartGovernanceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<OrgPositionDetailDto>> ListPositionsAsync(CancellationToken cancellationToken);

    Task<OrgPositionDetailDto?> GetPositionAsync(Guid id, CancellationToken cancellationToken);

    Task<OrgPositionDetailDto> UpdatePositionAsync(
        Guid id,
        UpsertOrgPositionRequest request,
        CancellationToken cancellationToken);

    Task<OrgPositionDetailDto> CreatePositionAsync(
        CreateOrgPositionRequest request,
        CancellationToken cancellationToken);

    Task DeletePositionAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrgDepartmentDto>> ListDepartmentsAsync(CancellationToken cancellationToken);

    Task<OrgDepartmentDto> CreateDepartmentAsync(
        UpsertOrgDepartmentRequest request,
        CancellationToken cancellationToken);

    Task<OrgDepartmentDto> UpdateDepartmentAsync(
        Guid id,
        UpsertOrgDepartmentRequest request,
        CancellationToken cancellationToken);

    Task DeleteDepartmentAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrgDepartmentMappingDto>> ListDepartmentMappingsAsync(CancellationToken cancellationToken);

    Task<OrgDepartmentMappingDto> UpdateDepartmentMappingAsync(
        Guid id,
        UpsertOrgDepartmentMappingRequest request,
        CancellationToken cancellationToken);

    Task<ImportDepartmentsFromDirectoryResultDto> ImportDepartmentsFromDirectoryAsync(
        ImportDepartmentsFromDirectoryRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken);

    Task<OrgChartGovernanceSummaryDto> ImportFromGraphAsync(
        ImportFromGraphRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken);
}
