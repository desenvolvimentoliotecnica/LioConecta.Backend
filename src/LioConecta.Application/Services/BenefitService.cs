using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class BenefitService(
    IBenefitRepository benefitRepository,
    IServiceRequestService serviceRequestService,
    ICurrentUserService currentUserService,
    IAppSettingsProvider settingsProvider) : IBenefitService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<BenefitSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var benefits = await benefitRepository.ListAsync(personId, cancellationToken);
        var dependentsCount = benefits
            .SelectMany(b => DeserializeDetails(b.DetailsJson).Dependents)
            .Count();

        return new BenefitSummaryDto(
            benefits.Count,
            benefits.Sum(b => b.MonthlyValue ?? 0m),
            dependentsCount);
    }

    public async Task<IReadOnlyList<BenefitListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var benefits = await benefitRepository.ListAsync(personId, cancellationToken);
        return benefits.Select(b => ToListItem(b)).ToList();
    }

    public async Task<BenefitDetailDto?> GetDetailAsync(
        string benefitId,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var benefit = await benefitRepository.GetByKeyAsync(personId, benefitId, cancellationToken);
        return benefit is null ? null : ToDetail(benefit);
    }

    public async Task<BenefitRequestResultDto> CreateRequestAsync(
        CreateBenefitRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var benefit = await benefitRepository.GetByKeyAsync(personId, request.BenefitId, cancellationToken)
            ?? throw new InvalidOperationException("Benefício não encontrado.");

        var payload = new Dictionary<string, object?>
        {
            ["benefitId"] = request.BenefitId,
            ["benefitTitle"] = benefit.Title,
            ["notes"] = request.Notes,
        };

        var created = await serviceRequestService.CreateAsync(
            new CreateServiceRequestRequest("servicos-beneficios", ServiceCategory.RH, payload),
            cancellationToken);

        return new BenefitRequestResultDto(
            created.Id,
            created.Status.ToString(),
            "Solicitação registrada com sucesso. O RH analisará a alteração ou informação sobre o benefício.");
    }

    private BenefitListItemDto ToListItem(Domain.Entities.EmployeeBenefit benefit) =>
        new(
            benefit.BenefitKey,
            benefit.Title,
            benefit.Desc,
            benefit.Category,
            benefit.Provider,
            benefit.Status,
            benefit.Featured,
            benefit.IsActive,
            ResolvePortalUrl(benefit),
            benefit.MonthlyValue);

    private BenefitDetailDto ToDetail(Domain.Entities.EmployeeBenefit benefit)
    {
        var details = DeserializeDetails(benefit.DetailsJson);
        return new BenefitDetailDto(
            benefit.BenefitKey,
            benefit.Title,
            benefit.Desc,
            benefit.Category,
            benefit.Provider,
            benefit.Status,
            benefit.Featured,
            ResolvePortalUrl(benefit),
            benefit.HelpText,
            benefit.MonthlyValue,
            details.Lines,
            details.Dependents,
            details.Notes);
    }

    private string? ResolvePortalUrl(Domain.Entities.EmployeeBenefit benefit)
    {
        var settingKey = BenefitPortalSettingCatalog.SettingKey(benefit.BenefitKey);
        if (settingsProvider.TryGetString(settingKey, out var configured))
        {
            return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();
        }

        return string.IsNullOrWhiteSpace(benefit.PortalUrl) ? null : benefit.PortalUrl.Trim();
    }

    private static BenefitDetailsPayload DeserializeDetails(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new BenefitDetailsPayload([], [], []);
        }

        var raw = JsonSerializer.Deserialize<BenefitDetailsRaw>(json, JsonOptions);
        if (raw is null)
        {
            return new BenefitDetailsPayload([], [], []);
        }

        var lines = raw.Lines?
            .Select(l => new BenefitDetailLineDto(l.Label ?? string.Empty, l.Amount, l.Note))
            .ToList() ?? [];

        var dependents = raw.Dependents?
            .Select(d => new BenefitDependentDto(d.Name ?? string.Empty, d.Relation ?? string.Empty, d.MonthlyValue))
            .ToList() ?? [];

        return new BenefitDetailsPayload(lines, dependents, raw.Notes ?? []);
    }

    private sealed record BenefitDetailsPayload(
        IReadOnlyList<BenefitDetailLineDto> Lines,
        IReadOnlyList<BenefitDependentDto> Dependents,
        IReadOnlyList<string> Notes);

    private sealed class BenefitDetailsRaw
    {
        public List<BenefitLineRaw>? Lines { get; set; }
        public List<BenefitDependentRaw>? Dependents { get; set; }
        public List<string>? Notes { get; set; }
    }

    private sealed class BenefitLineRaw
    {
        public string? Label { get; set; }
        public decimal? Amount { get; set; }
        public string? Note { get; set; }
    }

    private sealed class BenefitDependentRaw
    {
        public string? Name { get; set; }
        public string? Relation { get; set; }
        public decimal? MonthlyValue { get; set; }
    }
}
