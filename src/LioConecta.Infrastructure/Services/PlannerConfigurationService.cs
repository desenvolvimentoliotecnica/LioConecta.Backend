using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Integrations.Graph;

namespace LioConecta.Infrastructure.Services;

public sealed class PlannerConfigurationService(
    IAppSettingsProvider settingsProvider,
    IPlannerAdapter plannerAdapter,
    IAppSettingRepository appSettingRepository,
    PlannerConnectionTester connectionTester) : IPlannerConfigurationService
{
    public async Task<PlannerConnectionTestResponse> TestConnectionAsync(
        TestPlannerConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var plannerEnabled = settingsProvider.GetBool(AppSettingKeys.PlannerEnabled, false);
        var planId = FirstNonEmpty(request.PlanId, settingsProvider.GetString(AppSettingKeys.PlannerPlanId));

        if (!plannerEnabled)
        {
            return new PlannerConnectionTestResponse(
                false,
                "Integração Planner desabilitada.",
                "Ative «Planner — integração habilitada» nesta seção e salve.",
                UsesDevAdapters: false,
                PlannerEnabled: false,
                PlanId: planId,
                PlanTitle: null,
                BucketCount: null,
                TaskCount: null);
        }

        if (string.IsNullOrWhiteSpace(planId))
        {
            return new PlannerConnectionTestResponse(
                false,
                "ID do plano não informado.",
                "Informe o GUID do plano Microsoft Planner.",
                UsesDevAdapters: false,
                PlannerEnabled: true,
                PlanId: null,
                PlanTitle: null,
                BucketCount: null,
                TaskCount: null);
        }

        var graphConfigured =
            !string.IsNullOrWhiteSpace(settingsProvider.GetString(AppSettingKeys.GraphTenantId))
            && !string.IsNullOrWhiteSpace(settingsProvider.GetString(AppSettingKeys.GraphClientId))
            && !string.IsNullOrWhiteSpace(settingsProvider.GetString(AppSettingKeys.GraphClientSecret));

        if (!graphConfigured)
        {
            return new PlannerConnectionTestResponse(
                false,
                "Credenciais Microsoft Graph não configuradas.",
                "Configure tenant, client ID e secret na seção Microsoft Graph.",
                UsesDevAdapters: false,
                PlannerEnabled: true,
                PlanId: planId,
                PlanTitle: null,
                BucketCount: null,
                TaskCount: null);
        }

        var result = await connectionTester.TestAsync(planId.Trim(), plannerAdapter, cancellationToken);
        if (result.Success && !string.IsNullOrWhiteSpace(result.PlanTitle))
        {
            await PersistPlanMetadataAsync(planId.Trim(), result.PlanTitle, cancellationToken);
        }

        return result with { PlannerEnabled = plannerEnabled, PlanId = planId.Trim() };
    }

    private async Task PersistPlanMetadataAsync(
        string planId,
        string planTitle,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        await UpsertValueAsync(AppSettingKeys.PlannerPlanTitle, planTitle, cancellationToken);
        await UpsertValueAsync(AppSettingKeys.PlannerLastSyncUtc, now, cancellationToken);
    }

    private async Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        var existing = await appSettingRepository.GetByKeyAsync(key, cancellationToken);
        if (existing is null)
        {
            return;
        }

        existing.Value = value;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await appSettingRepository.UpsertAsync(existing, cancellationToken);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
