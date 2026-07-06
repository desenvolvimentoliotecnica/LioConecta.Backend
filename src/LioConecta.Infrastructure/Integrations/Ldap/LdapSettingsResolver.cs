using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;

namespace LioConecta.Infrastructure.Integrations.Ldap;

public sealed record LdapRuntimeSettings(
    bool Enabled,
    string Host,
    int Port,
    bool UseSsl,
    string BindDn,
    string BindPassword,
    string SearchBase,
    string UserFilter,
    string Domain);

public sealed class LdapSettingsResolver(IAppSettingsProvider settingsProvider)
{
    private const string SecretMask = "********";

    public LdapRuntimeSettings Resolve(
        string? host = null,
        int? port = null,
        bool? useSsl = null,
        string? bindDn = null,
        string? bindPassword = null,
        string? searchBase = null)
    {
        var resolvedPort = port ?? ParseInt(settingsProvider.GetString(AppSettingKeys.LdapPort), 389);
        return new LdapRuntimeSettings(
            settingsProvider.GetBool(AppSettingKeys.LdapEnabled, false),
            FirstNonEmpty(host, settingsProvider.GetString(AppSettingKeys.LdapHost)),
            resolvedPort,
            useSsl ?? settingsProvider.GetBool(AppSettingKeys.LdapUseSsl, false),
            FirstNonEmpty(bindDn, settingsProvider.GetString(AppSettingKeys.LdapBindDn)),
            ResolveSecret(bindPassword, AppSettingKeys.LdapBindPassword),
            FirstNonEmpty(searchBase, settingsProvider.GetString(AppSettingKeys.LdapSearchBase)),
            settingsProvider.GetString(AppSettingKeys.LdapUserFilter, "(userPrincipalName={0})"),
            settingsProvider.GetString(AppSettingKeys.LdapDomain, "@liotecnica.com.br"));
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

    private static int ParseInt(string? raw, int fallback) =>
        int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
}
