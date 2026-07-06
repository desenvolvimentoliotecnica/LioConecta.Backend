using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class AppSettingService(
    IAppSettingRepository repository,
    IAppSettingsProvider settingsProvider,
    ICurrentUserService currentUserService) : IAppSettingService
{
    private const string SecretMask = "********";

    private static readonly Dictionary<string, (string Label, string? Description)> CategoryMeta =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["database"] = ("Banco de dados", "PostgreSQL — conexão e parâmetros do banco principal."),
            ["redis"] = ("Redis", "Cache e backplane SignalR."),
            ["azure_ad"] = ("Azure AD", "Autenticação Entra ID / JWT."),
            ["auth"] = ("Autenticação", "Políticas de auth e modo desenvolvimento."),
            ["cors"] = ("CORS", "Origens permitidas do front-end."),
            ["integrations"] = ("Integrações", "Feature flags de adaptadores externos."),
            ["totvs"] = ("TOTVS", "ERP — endpoints e credenciais."),
            ["glpi"] = ("GLPI", "Service desk — endpoints e tokens."),
            ["graph"] = ("Microsoft Graph", "SharePoint, calendário, Planner e presença."),
            ["planner"] = ("Microsoft Planner", "Plano da equipe exibido em Minhas Atividades — reutiliza credenciais Graph."),
            ["workers"] = ("Workers", "Intervalos de sincronização em background."),
            ["serilog"] = ("Logging", "Níveis de log Serilog."),
            ["media"] = ("Mídia", "Uploads locais de imagens de comunicados (até migrar para S3/SharePoint)."),
            ["benefits"] = ("Benefícios", "URLs dos portais externos exibidos na página de Benefícios (botão Abrir portal)."),
            ["leave"] = ("Férias e ausências", "URLs dos portais e links externos dos serviços de férias, licenças e afastamentos."),
        };

    private static readonly HashSet<string> RestartKeys =
    [
        AppSettingKeys.DatabaseDefaultConnection,
        AppSettingKeys.RedisConnection,
        AppSettingKeys.AzureAdTenantId,
        AppSettingKeys.AzureAdClientId,
        AppSettingKeys.AzureAdAudience,
        AppSettingKeys.AzureAdInstance,
        AppSettingKeys.AuthUseDevAuth,
        AppSettingKeys.IntegrationsUseDevAdapters,
        AppSettingKeys.GraphTenantId,
        AppSettingKeys.GraphClientId,
        AppSettingKeys.GraphClientSecret,
    ];

    public async Task<IReadOnlyList<AppSettingCategoryDto>> GetGroupedAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetAllAsync(cancellationToken);
        var byKey = rows.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

        var categories = AppSettingCatalog.All
            .GroupBy(d => d.Category)
            .OrderBy(g => CategoryOrder(g.Key))
            .Select(g =>
            {
                var meta = CategoryMeta.GetValueOrDefault(g.Key);
                return new AppSettingCategoryDto
                {
                    Id = g.Key,
                    Label = meta.Label ?? g.Key,
                    Description = meta.Description,
                    Settings = g
                        .OrderBy(d => d.SortOrder)
                        .Select(def => MapDto(def, byKey.GetValueOrDefault(def.Key)))
                        .ToList(),
                };
            })
            .ToList();

        return categories;
    }

    public async Task<AppSettingsUpdateResultDto> BulkUpdateAsync(
        BulkUpdateAppSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var existing = (await repository.GetAllAsync(cancellationToken))
            .ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

        var requiresRestart = false;
        var updates = new List<AppSetting>();

        foreach (var item in request.Settings)
        {
            var def = AppSettingCatalog.All.FirstOrDefault(d =>
                string.Equals(d.Key, item.Key, StringComparison.OrdinalIgnoreCase));

            if (def is null)
            {
                continue;
            }

            var current = existing.GetValueOrDefault(def.Key);
            var incoming = item.Value?.Trim() ?? string.Empty;

            if (def.IsSecret && (string.IsNullOrEmpty(incoming) || incoming == SecretMask))
            {
                continue;
            }

            if (current is not null &&
                string.Equals(current.Value, incoming, StringComparison.Ordinal))
            {
                continue;
            }

            if (RestartKeys.Contains(def.Key))
            {
                requiresRestart = true;
            }

            updates.Add(new AppSetting
            {
                Id = current?.Id ?? Guid.NewGuid(),
                Key = def.Key,
                Category = def.Category,
                Label = def.Label,
                Description = def.Description,
                Value = incoming,
                ValueType = def.ValueType,
                IsSecret = def.IsSecret,
                SortOrder = def.SortOrder,
                UpdatedById = personId,
                CreatedAt = current?.CreatedAt ?? now,
                UpdatedAt = now,
            });
        }

        if (updates.Count > 0)
        {
            await repository.UpsertManyAsync(updates, cancellationToken);

            var allRows = await repository.GetAllAsync(cancellationToken);
            settingsProvider.Reload(allRows.ToDictionary(r => r.Key, r => r.Value));
        }

        var categories = await GetGroupedAsync(cancellationToken);

        return new AppSettingsUpdateResultDto
        {
            Categories = categories,
            RequiresRestart = requiresRestart,
            Message = requiresRestart
                ? "Alterações salvas. Reinicie a API para aplicar configurações de infraestrutura."
                : "Configurações salvas com sucesso.",
        };
    }

    private static AppSettingDto MapDto(AppSettingDefinition def, AppSetting? row)
    {
        var hasValue = row is not null && !string.IsNullOrEmpty(row.Value);
        var displayValue = def.IsSecret
            ? hasValue ? SecretMask : string.Empty
            : row?.Value ?? def.DefaultValue;

        return new AppSettingDto
        {
            Key = def.Key,
            Category = def.Category,
            Label = def.Label,
            Description = def.Description,
            Value = displayValue,
            ValueType = def.ValueType,
            IsSecret = def.IsSecret,
            HasValue = hasValue,
            SortOrder = def.SortOrder,
            UpdatedAt = row?.UpdatedAt,
        };
    }

    private static int CategoryOrder(string category) => category switch
    {
        "database" => 1,
        "redis" => 2,
        "azure_ad" => 3,
        "auth" => 4,
        "cors" => 5,
        "integrations" => 6,
        "totvs" => 7,
        "glpi" => 8,
        "graph" => 9,
        "workers" => 10,
        "serilog" => 11,
        "media" => 12,
        "benefits" => 13,
        "leave" => 14,
        _ => 99,
    };
}
