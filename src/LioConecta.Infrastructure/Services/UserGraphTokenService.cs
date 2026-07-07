using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Services;

public sealed class UserGraphTokenService(
    IUserTeamsTokenRepository tokenRepository,
    IAppSettingsProvider settingsProvider,
    ILogger<UserGraphTokenService> logger) : IUserGraphTokenService, IUserTeamsTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> HasLinkedAccountAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var token = await tokenRepository.GetByPersonIdAsync(personId, cancellationToken);
        return token is not null && !string.IsNullOrWhiteSpace(token.EncryptedRefreshToken);
    }

    public async Task<bool> HasScopeAsync(Guid personId, string scope, CancellationToken cancellationToken = default)
    {
        var token = await tokenRepository.GetByPersonIdAsync(personId, cancellationToken);
        if (token is null)
        {
            return false;
        }

        var scopes = DeserializeScopes(token.ScopesJson);
        var normalized = NormalizeScope(scope);
        return scopes.Any(s => string.Equals(NormalizeScope(s), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public async Task StoreTokensAsync(
        Guid personId,
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAt,
        IReadOnlyList<string>? scopes,
        CancellationToken cancellationToken = default)
    {
        var encryptionKey = RequireEncryptionKey();
        var now = DateTimeOffset.UtcNow;
        var existing = await tokenRepository.GetByPersonIdAsync(personId, cancellationToken);
        var mergedScopes = MergeScopes(
            existing is null ? [] : DeserializeScopes(existing.ScopesJson),
            scopes ?? []);

        string encryptedRefresh;
        if (string.IsNullOrWhiteSpace(refreshToken) && existing is not null)
        {
            encryptedRefresh = existing.EncryptedRefreshToken;
        }
        else
        {
            encryptedRefresh = SecretProtector.Protect(refreshToken, encryptionKey);
        }

        var entity = new UserTeamsToken
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            PersonId = personId,
            EncryptedAccessToken = SecretProtector.Protect(accessToken, encryptionKey),
            EncryptedRefreshToken = encryptedRefresh,
            ExpiresAt = expiresAt,
            ScopesJson = JsonSerializer.Serialize(mergedScopes, JsonOptions),
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now
        };

        await tokenRepository.UpsertAsync(entity, cancellationToken);
    }

    public async Task<string> GetValidAccessTokenAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var token = await tokenRepository.GetByPersonIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Conta Microsoft não vinculada.");

        var encryptionKey = RequireEncryptionKey();
        var accessToken = SecretProtector.Unprotect(token.EncryptedAccessToken, encryptionKey);

        if (DateTimeOffset.UtcNow < token.ExpiresAt.AddMinutes(-5))
        {
            return accessToken;
        }

        var refreshToken = SecretProtector.Unprotect(token.EncryptedRefreshToken, encryptionKey);
        var refreshed = await RefreshAccessTokenAsync(refreshToken, cancellationToken);

        token.EncryptedAccessToken = SecretProtector.Protect(refreshed.AccessToken, encryptionKey);
        if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
        {
            token.EncryptedRefreshToken = SecretProtector.Protect(refreshed.RefreshToken, encryptionKey);
        }

        token.ExpiresAt = refreshed.ExpiresAt;
        token.UpdatedAt = DateTimeOffset.UtcNow;
        await tokenRepository.UpsertAsync(token, cancellationToken);

        return refreshed.AccessToken;
    }

    public Task UnlinkAsync(Guid personId, CancellationToken cancellationToken = default) =>
        tokenRepository.DeleteByPersonIdAsync(personId, cancellationToken);

    private string RequireEncryptionKey()
    {
        var calendarKey = settingsProvider.GetString(AppSettingKeys.CalendarTokenEncryptionKey);
        if (!string.IsNullOrWhiteSpace(calendarKey))
        {
            return calendarKey;
        }

        var chatKey = settingsProvider.GetString(AppSettingKeys.ChatTeamsTokenEncryptionKey);
        if (!string.IsNullOrWhiteSpace(chatKey))
        {
            return chatKey;
        }

        throw new InvalidOperationException(
            $"Configure '{AppSettingKeys.CalendarTokenEncryptionKey}' (calendário) ou '{AppSettingKeys.ChatTeamsTokenEncryptionKey}' (chat) para armazenar tokens Microsoft delegados.");
    }

    private async Task<(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt)> RefreshAccessTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tenantId = settingsProvider.GetString(AppSettingKeys.AzureAdTenantId);
        var clientId = settingsProvider.GetString(AppSettingKeys.AzureAdClientId);
        var clientSecret = settingsProvider.GetString(AppSettingKeys.GraphClientSecret);
        var scopes = ResolveRefreshScopes();

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Azure AD tenant e client ID são obrigatórios para renovar tokens Microsoft.");
        }

        using var client = new HttpClient();
        var tokenUri = new Uri(
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId.Trim())}/oauth2/v2.0/token");

        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId.Trim(),
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = scopes
        };

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            form["client_secret"] = clientSecret;
        }

        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync(tokenUri, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Microsoft token refresh failed ({StatusCode}): {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(
                "Não foi possível renovar o token Microsoft. Vincule a conta novamente.");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Resposta OAuth não contém access_token.");

        var newRefreshToken = root.TryGetProperty("refresh_token", out var refreshProp)
            ? refreshProp.GetString()
            : null;

        var expiresIn = root.TryGetProperty("expires_in", out var expiresProp)
            ? expiresProp.GetInt32()
            : 3600;

        return (accessToken, newRefreshToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
    }

    private string ResolveRefreshScopes()
    {
        var allScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddScopesFromSetting(allScopes, AppSettingKeys.ChatTeamsDelegatedScopes, [
            "Chat.ReadWrite", "User.Read", "offline_access"
        ]);
        AddScopesFromSetting(allScopes, AppSettingKeys.CalendarDelegatedScopes, [
            "Calendars.ReadWrite", "User.Read", "offline_access"
        ]);

        if (allScopes.Count == 0)
        {
            return "https://graph.microsoft.com/Calendars.ReadWrite https://graph.microsoft.com/User.Read offline_access";
        }

        return string.Join(' ', allScopes.Select(NormalizeScope));
    }

    private void AddScopesFromSetting(HashSet<string> target, string key, string[] defaults)
    {
        var raw = settingsProvider.GetString(key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            foreach (var scope in defaults)
            {
                target.Add(NormalizeScope(scope));
            }

            return;
        }

        try
        {
            var scopes = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            if (scopes.Length == 0)
            {
                foreach (var scope in defaults)
                {
                    target.Add(NormalizeScope(scope));
                }

                return;
            }

            foreach (var scope in scopes)
            {
                target.Add(NormalizeScope(scope));
            }
        }
        catch (JsonException)
        {
            target.Add(NormalizeScope(raw));
        }
    }

    private static IReadOnlyList<string> MergeScopes(
        IReadOnlyList<string> existing,
        IReadOnlyList<string> incoming)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var scope in existing)
        {
            if (!string.IsNullOrWhiteSpace(scope))
            {
                merged.Add(NormalizeScope(scope));
            }
        }

        foreach (var scope in incoming)
        {
            if (!string.IsNullOrWhiteSpace(scope))
            {
                merged.Add(NormalizeScope(scope));
            }
        }

        return merged.ToList();
    }

    private static IReadOnlyList<string> DeserializeScopes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string NormalizeScope(string scope)
    {
        var trimmed = scope.Trim();
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (string.Equals(trimmed, "offline_access", StringComparison.OrdinalIgnoreCase))
        {
            return "offline_access";
        }

        return $"https://graph.microsoft.com/{trimmed}";
    }
}
