using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class LoopService(IAppSettingsProvider settingsProvider) : ILoopService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<LoopBootstrapDto> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var enabled = settingsProvider.GetBool(AppSettingKeys.LoopEnabled, true);
        var rolesJson = settingsProvider.GetString(
            AppSettingKeys.LoopAllowedRoles,
            "[\"Manager\",\"Admin\",\"AnalyticsViewer\"]");
        var emailsJson = settingsProvider.GetString(AppSettingKeys.LoopAllowedEmails, "[]");

        var bootstrap = new LoopBootstrapDto(
            enabled,
            DeserializeRoles(rolesJson),
            DeserializeEmails(emailsJson));

        return Task.FromResult(bootstrap);
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
