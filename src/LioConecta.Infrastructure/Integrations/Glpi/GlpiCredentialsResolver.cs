using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed class GlpiCredentialsResolver(IAppSettingsProvider settingsProvider)
{
    private const string SecretMask = "********";

    public GlpiRuntimeCredentials Resolve(
        string? baseUrl = null,
        string? appToken = null,
        string? userToken = null)
    {
        var resolvedBaseUrl = FirstNonEmpty(
            baseUrl,
            settingsProvider.GetString(AppSettingKeys.GlpiBaseUrl));

        var resolvedAppToken = ResolveSecret(appToken, AppSettingKeys.GlpiAppToken);
        var resolvedUserToken = ResolveSecret(userToken, AppSettingKeys.GlpiUserToken);

        var portalUrl = FirstNonEmpty(
            settingsProvider.GetString(AppSettingKeys.GlpiPortalUrl),
            DerivePortalUrl(resolvedBaseUrl));

        var profileId = ResolveProfileId(settingsProvider.GetString(AppSettingKeys.GlpiProfileId));

        return new GlpiRuntimeCredentials(
            resolvedBaseUrl.TrimEnd('/'),
            resolvedAppToken,
            resolvedUserToken,
            portalUrl.TrimEnd('/'),
            profileId);
    }

    private string ResolveSecret(string? incoming, string key)
    {
        if (!string.IsNullOrWhiteSpace(incoming)
            && !string.Equals(incoming.Trim(), SecretMask, StringComparison.Ordinal))
        {
            return incoming.Trim();
        }

        return settingsProvider.GetString(key);
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

    private static int? ResolveProfileId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), out var profileId) && profileId > 0
            ? profileId
            : null;
    }

    private static string DerivePortalUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        var uri = new Uri(baseUrl.TrimEnd('/'));
        return $"{uri.Scheme}://{uri.Host}";
    }
}
