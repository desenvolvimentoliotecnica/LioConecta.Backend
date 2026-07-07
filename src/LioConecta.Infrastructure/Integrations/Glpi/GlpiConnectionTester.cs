using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LioConecta.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed class GlpiSessionManager(ILogger<GlpiSessionManager> logger)
{
    private const int SessionDefaultProfileSentinel = -1;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _sessionToken;
    private string? _sessionKey;
    private int? _activeProfileId;
    private DateTimeOffset _sessionExpiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetSessionTokenAsync(
        HttpClient httpClient,
        GlpiRuntimeCredentials credentials,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{credentials.BaseUrl}|{credentials.AppToken}|{credentials.UserToken}|{credentials.ProfileId}";
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_sessionToken is not null
                && string.Equals(_sessionKey, cacheKey, StringComparison.Ordinal)
                && _sessionExpiresAt > DateTimeOffset.UtcNow
                && ProfileMatches(credentials.ProfileId))
            {
                return _sessionToken;
            }

            if (_sessionToken is not null)
            {
                await TryKillSessionAsync(httpClient, credentials, _sessionToken, cancellationToken);
            }

            _activeProfileId = null;

            var initUrl = BuildUrl(credentials.BaseUrl, "initSession");
            using var request = new HttpRequestMessage(HttpMethod.Get, initUrl);
            request.Headers.Add("App-Token", credentials.AppToken);
            request.Headers.TryAddWithoutValidation("Authorization", $"user_token {credentials.UserToken}");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"GLPI initSession falhou ({(int)response.StatusCode}): {TrimBody(body)}");
            }

            using var document = JsonDocument.Parse(body);
            var token = document.RootElement.TryGetProperty("session_token", out var tokenElement)
                ? tokenElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("GLPI initSession não retornou session_token.");
            }

            _sessionToken = token;
            _sessionKey = cacheKey;
            _sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(14);
            await EnsureActiveProfileAsync(httpClient, credentials, _sessionToken, cancellationToken);
            return _sessionToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task InvalidateSessionAsync(
        HttpClient httpClient,
        GlpiRuntimeCredentials credentials,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_sessionToken is null)
            {
                return;
            }

            await TryKillSessionAsync(httpClient, credentials, _sessionToken, cancellationToken);
            _sessionToken = null;
            _sessionKey = null;
            _activeProfileId = null;
            _sessionExpiresAt = DateTimeOffset.MinValue;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool ProfileMatches(int? profileId)
    {
        if (profileId is null or <= 0)
        {
            return _activeProfileId is null or SessionDefaultProfileSentinel;
        }

        if (_activeProfileId == profileId)
        {
            return true;
        }

        return _activeProfileId == SessionDefaultProfileSentinel;
    }

    private async Task EnsureActiveProfileAsync(
        HttpClient httpClient,
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        if (credentials.ProfileId is null or <= 0)
        {
            return;
        }

        if (_activeProfileId == credentials.ProfileId)
        {
            return;
        }

        var changeUrl = BuildUrl(credentials.BaseUrl, "changeActiveProfile");
        using var request = new HttpRequestMessage(HttpMethod.Post, changeUrl);
        request.Headers.Add("App-Token", credentials.AppToken);
        request.Headers.Add("Session-Token", sessionToken);
        request.Content = JsonContent.Create(new { profiles_id = credentials.ProfileId.Value });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is System.Net.HttpStatusCode.NotFound
                or System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogWarning(
                    "GLPI changeActiveProfile ignorado: profiles_id={ProfileId} indisponível para o usuário de serviço ({Status}). Usando perfil padrão da sessão. Defina glpi.profile_id=0 ou um ID válido (ex.: Gestor=7). Resposta: {Body}",
                    credentials.ProfileId,
                    (int)response.StatusCode,
                    TrimBody(body));
                _activeProfileId = SessionDefaultProfileSentinel;
                return;
            }

            throw new InvalidOperationException(
                $"GLPI changeActiveProfile falhou ({(int)response.StatusCode}): {TrimBody(body)}");
        }

        if (!body.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "GLPI não aceitou profiles_id={ProfileId}: {Body}. Mantendo perfil padrão da sessão.",
                credentials.ProfileId,
                TrimBody(body));
            _activeProfileId = SessionDefaultProfileSentinel;
            return;
        }

        _activeProfileId = credentials.ProfileId;
        logger.LogDebug("GLPI perfil ativo definido para profiles_id={ProfileId}", credentials.ProfileId);
    }

    private async Task TryKillSessionAsync(
        HttpClient httpClient,
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var killUrl = BuildUrl(credentials.BaseUrl, "killSession");
            using var request = new HttpRequestMessage(HttpMethod.Get, killUrl);
            request.Headers.Add("App-Token", credentials.AppToken);
            request.Headers.Add("Session-Token", sessionToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("GLPI killSession retornou {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Falha ao encerrar sessão GLPI.");
        }
    }

    private static string BuildUrl(string baseUrl, string path) =>
        $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

    private static string TrimBody(string body) =>
        body.Length > 400 ? body[..400] + "…" : body;
}

public sealed class GlpiConnectionTester(GlpiSessionManager sessionManager, ILogger<GlpiConnectionTester> logger)
{
    public async Task<GlpiConnectionTestResponse> TestAsync(
        GlpiRuntimeCredentials credentials,
        bool usesDevAdapters,
        CancellationToken cancellationToken)
    {
        if (usesDevAdapters)
        {
            return new GlpiConnectionTestResponse(
                false,
                "Adaptadores mock estão ativos — o GLPI real não é consultado.",
                "Em Configurações do Backend → Integrações, defina «Modo mock» como Não (false) e reinicie a API.",
                UsesDevAdapters: true);
        }

        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            return Fail(usesDevAdapters, "URL base do GLPI não informada.", null);
        }

        if (string.IsNullOrWhiteSpace(credentials.AppToken))
        {
            return Fail(usesDevAdapters, "App token do GLPI não informado.", null);
        }

        if (string.IsNullOrWhiteSpace(credentials.UserToken))
        {
            return Fail(usesDevAdapters, "User token do GLPI não informado.", null);
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var sessionToken = await sessionManager.GetSessionTokenAsync(client, credentials, cancellationToken);
            await sessionManager.InvalidateSessionAsync(client, credentials, cancellationToken);

            return new GlpiConnectionTestResponse(
                true,
                "Conexão com GLPI realizada com sucesso.",
                $"initSession OK (session_token obtido, {sessionToken.Length} caracteres).",
                UsesDevAdapters: false);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao testar conexão GLPI.");
            var detail = exception.Message;
            if (detail.Contains("401", StringComparison.Ordinal) || detail.Contains("UNAUTHORIZED", StringComparison.OrdinalIgnoreCase))
            {
                detail += " Verifique se o app token (Lioconecta) e o user token (glpi_system_service) não estão invertidos.";
            }

            return Fail(
                usesDevAdapters,
                "Não foi possível autenticar no GLPI.",
                detail);
        }
    }

    private static GlpiConnectionTestResponse Fail(bool usesDevAdapters, string message, string? detail) =>
        new(false, message, detail, usesDevAdapters);
}
