using System.DirectoryServices.Protocols;
using System.Net;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Ldap;

public sealed class LdapAuthService(
    LdapSettingsResolver settingsResolver,
    ILogger<LdapAuthService> logger) : ILdapAuthService
{
    public Task<LdapAuthResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = settingsResolver.Resolve();
        if (!settings.Enabled)
        {
            return Task.FromResult<LdapAuthResult?>(null);
        }

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            throw new InvalidOperationException("LDAP está habilitado, mas o servidor não foi configurado.");
        }

        var normalizedEmail = NormalizeEmail(email, settings.Domain);
        try
        {
            using var connection = CreateConnection(settings);
            connection.Credential = new NetworkCredential(normalizedEmail, password);
            connection.AuthType = AuthType.Basic;
            connection.Bind();

            var displayName = TryResolveDisplayName(connection, settings, normalizedEmail);
            return Task.FromResult<LdapAuthResult?>(new LdapAuthResult(normalizedEmail, displayName));
        }
        catch (LdapException exception)
        {
            logger.LogWarning(exception, "Falha de autenticação LDAP para {Email}.", normalizedEmail);
            return Task.FromResult<LdapAuthResult?>(null);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro inesperado na autenticação LDAP para {Email}.", normalizedEmail);
            throw;
        }
    }

    internal static LdapConnection CreateConnection(LdapRuntimeSettings settings)
    {
        var identifier = new LdapDirectoryIdentifier(settings.Host, settings.Port, false, false);
        var connection = new LdapConnection(identifier)
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        if (settings.UseSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }

        return connection;
    }

    internal static string NormalizeEmail(string email, string domain)
    {
        var trimmed = email.Trim();
        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            return trimmed.ToLowerInvariant();
        }

        var suffix = string.IsNullOrWhiteSpace(domain)
            ? string.Empty
            : domain.StartsWith('@') ? domain : $"@{domain}";

        return $"{trimmed}{suffix}".ToLowerInvariant();
    }

    private static string? TryResolveDisplayName(
        LdapConnection connection,
        LdapRuntimeSettings settings,
        string email)
    {
        if (string.IsNullOrWhiteSpace(settings.SearchBase)
            || string.IsNullOrWhiteSpace(settings.BindDn)
            || string.IsNullOrWhiteSpace(settings.BindPassword))
        {
            return null;
        }

        try
        {
            using var searchConnection = CreateConnection(settings);
            searchConnection.Credential = new NetworkCredential(settings.BindDn, settings.BindPassword);
            searchConnection.AuthType = AuthType.Basic;
            searchConnection.Bind();

            var filter = string.Format(settings.UserFilter, EscapeFilterValue(email));
            var request = new SearchRequest(
                settings.SearchBase,
                filter,
                SearchScope.Subtree,
                "displayName",
                "cn",
                "mail");

            var response = (SearchResponse)searchConnection.SendRequest(request);
            var entry = response.Entries.Cast<SearchResultEntry>().FirstOrDefault();
            if (entry is null)
            {
                return null;
            }

            return entry.Attributes["displayName"]?[0]?.ToString()
                ?? entry.Attributes["cn"]?[0]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string EscapeFilterValue(string value) =>
        value
            .Replace("\\", "\\5c", StringComparison.Ordinal)
            .Replace("*", "\\2a", StringComparison.Ordinal)
            .Replace("(", "\\28", StringComparison.Ordinal)
            .Replace(")", "\\29", StringComparison.Ordinal)
            .Replace("\0", "\\00", StringComparison.Ordinal);
}
