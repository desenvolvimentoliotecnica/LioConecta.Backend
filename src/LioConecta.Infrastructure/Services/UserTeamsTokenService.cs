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

public sealed class UserTeamsTokenService(
    IUserTeamsTokenRepository tokenRepository,
    IAppSettingsProvider settingsProvider,
    ILogger<UserTeamsTokenService> logger) : IUserTeamsTokenService
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

        var entity = new UserTeamsToken
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            EncryptedAccessToken = SecretProtector.Protect(accessToken, encryptionKey),
            EncryptedRefreshToken = SecretProtector.Protect(refreshToken, encryptionKey),
            ExpiresAt = expiresAt,
            ScopesJson = JsonSerializer.Serialize(scopes ?? [], JsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        var existing = await tokenRepository.GetByPersonIdAsync(personId, cancellationToken);
        if (existing is not null)
        {
            entity.Id = existing.Id;
            entity.CreatedAt = existing.CreatedAt;
        }

        await tokenRepository.UpsertAsync(entity, cancellationToken);
    }

    public async Task<string> GetValidAccessTokenAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var token = await tokenRepository.GetByPersonIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Conta Microsoft Teams não vinculada.");

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
        var key = settingsProvider.GetString(AppSettingKeys.ChatTeamsTokenEncryptionKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                $"App setting '{AppSettingKeys.ChatTeamsTokenEncryptionKey}' is required for Teams chat tokens.");
        }

        return key;
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
            throw new InvalidOperationException("Azure AD tenant e client ID são obrigatórios para renovar tokens Teams.");
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
            logger.LogWarning("Teams token refresh failed ({StatusCode}): {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(
                "Não foi possível renovar o token Microsoft Teams. Vincule a conta novamente.");
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
        var raw = settingsProvider.GetString(AppSettingKeys.ChatTeamsDelegatedScopes);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "https://graph.microsoft.com/Chat.ReadWrite https://graph.microsoft.com/User.Read offline_access";
        }

        try
        {
            var scopes = JsonSerializer.Deserialize<string[]>(raw, JsonOptions) ?? [];
            if (scopes.Length == 0)
            {
                return "https://graph.microsoft.com/Chat.ReadWrite https://graph.microsoft.com/User.Read offline_access";
            }

            return string.Join(' ', scopes.Select(NormalizeScope));
        }
        catch (JsonException)
        {
            return raw;
        }
    }

    private static string NormalizeScope(string scope)
    {
        var trimmed = scope.Trim();
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return $"https://graph.microsoft.com/{trimmed}";
    }
}
