using System.Net.Http.Headers;
using System.Text.Json;
using LioConecta.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class GraphConnectionTester(ILogger<GraphConnectionTester> logger)
{
    private const string DirectoryUserSelect = "id,userPrincipalName,mail,accountEnabled";

    public async Task<GraphConnectionTestResponse> TestAsync(
        GraphRuntimeCredentials credentials,
        bool usesDevAdapters,
        CancellationToken cancellationToken)
    {
        if (usesDevAdapters)
        {
            return new GraphConnectionTestResponse(
                false,
                "Adaptadores mock estão ativos — o worker de diretório usa dados fictícios (3 usuários).",
                "Em Configurações do Backend → Integrações, defina «Modo mock» como Não (false) e reinicie a API.",
                UsesDevAdapters: true,
                DomainUserCount: null,
                TenantUserCount: null);
        }

        if (string.IsNullOrWhiteSpace(credentials.TenantId))
        {
            return Fail(usesDevAdapters, "Tenant ID do Graph não informado.", null);
        }

        if (string.IsNullOrWhiteSpace(credentials.ClientId))
        {
            return Fail(usesDevAdapters, "Client ID do Graph não informado.", null);
        }

        if (string.IsNullOrWhiteSpace(credentials.ClientSecret))
        {
            return Fail(usesDevAdapters, "Client secret do Graph não informado.", null);
        }

        try
        {
            var token = await AcquireTokenAsync(credentials, cancellationToken);
            using var client = new HttpClient
            {
                BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"),
                Timeout = TimeSpan.FromSeconds(60),
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var domain = credentials.EmailDomain.Trim().TrimStart('@').ToLowerInvariant();
            var domainUsers = 0;
            var tenantUsers = 0;
            var url = $"users?$select={DirectoryUserSelect}&$top=999";

            while (!string.IsNullOrWhiteSpace(url))
            {
                using var response = await client.GetAsync(url, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return Fail(
                        usesDevAdapters,
                        "Microsoft Graph rejeitou a consulta de usuários.",
                        $"HTTP {(int)response.StatusCode}: {TryReadGraphError(body)}");
                }

                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.TryGetProperty("value", out var valueElement))
                {
                    foreach (var item in valueElement.EnumerateArray())
                    {
                        tenantUsers++;
                        if (BelongsToDomain(
                                ReadString(item, "userPrincipalName"),
                                ReadString(item, "mail"),
                                domain)
                            && IsActiveUser(item))
                        {
                            domainUsers++;
                        }
                    }
                }

                url = root.TryGetProperty("@odata.nextLink", out var nextLink)
                    ? nextLink.GetString()
                    : null;
            }

            var detail =
                $"Token OAuth OK. Usuários no tenant: {tenantUsers}. Colaboradores @{domain} ativos: {domainUsers}. " +
                "Permissão necessária: User.Read.All (application).";

            return new GraphConnectionTestResponse(
                true,
                "Conexão com Microsoft Graph realizada com sucesso.",
                detail,
                UsesDevAdapters: false,
                DomainUserCount: domainUsers,
                TenantUserCount: tenantUsers);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao testar conexão Microsoft Graph.");
            return Fail(
                usesDevAdapters,
                "Não foi possível autenticar ou consultar o Microsoft Graph.",
                exception.Message);
        }
    }

    private static GraphConnectionTestResponse Fail(
        bool usesDevAdapters,
        string message,
        string? detail) =>
        new(false, message, detail, usesDevAdapters, null, null);

    private static async Task<string> AcquireTokenAsync(
        GraphRuntimeCredentials credentials,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var tokenUri = new Uri(
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(credentials.TenantId.Trim())}/oauth2/v2.0/token");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = credentials.ClientId.Trim(),
            ["client_secret"] = credentials.ClientSecret,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["grant_type"] = "client_credentials",
        });

        using var response = await client.PostAsync(tokenUri, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Token OAuth falhou ({(int)response.StatusCode}): {TryReadGraphError(body)}");
        }

        using var document = JsonDocument.Parse(body);
        var token = document.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Resposta OAuth não contém access_token.");
        }

        return token;
    }

    private static bool BelongsToDomain(string? upn, string? mail, string domain)
    {
        var suffix = "@" + domain;
        return (!string.IsNullOrWhiteSpace(upn) && upn.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
               || (!string.IsNullOrWhiteSpace(mail) && mail.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsActiveUser(JsonElement item) =>
        !item.TryGetProperty("accountEnabled", out var enabledElement)
        || enabledElement.ValueKind != JsonValueKind.False;

    private static string? ReadString(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string TryReadGraphError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? body;
                }
            }

            if (document.RootElement.TryGetProperty("error_description", out var description))
            {
                return description.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return body.Length > 400 ? body[..400] + "…" : body;
    }
}
