using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class LoopService(
    IAppSettingsProvider settingsProvider,
    IPermissionService permissionService) : ILoopService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<LoopBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var enabled = settingsProvider.GetBool(AppSettingKeys.LoopEnabled, true);
        var rolesJson = settingsProvider.GetString(
            AppSettingKeys.LoopAllowedRoles,
            "[\"Manager\",\"Admin\",\"AnalyticsViewer\"]");
        var emailsJson = settingsProvider.GetString(AppSettingKeys.LoopAllowedEmails, "[]");
        var canAccess = enabled
            && await permissionService.HasPermissionAsync("loop.access", DataScope.Global, cancellationToken);

        return new LoopBootstrapDto(
            enabled,
            canAccess,
            DeserializeRoles(rolesJson),
            DeserializeEmails(emailsJson));
    }

    private static IReadOnlyList<string> DeserializeRoles(string raw)
    {
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            var roles = new List<string>();

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (Enum.TryParse<UserRole>(value.Trim(), true, out _))
                {
                    roles.Add(value.Trim());
                }
            }

            return roles.Count > 0
                ? roles
                : ["Manager", "Admin", "AnalyticsViewer"];
        }
        catch
        {
            return ["Manager", "Admin", "AnalyticsViewer"];
        }
    }

    private static IReadOnlyList<string> DeserializeEmails(string raw)
    {
        try
        {
            var values = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            return values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
