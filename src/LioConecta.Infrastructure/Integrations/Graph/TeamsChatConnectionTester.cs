using LioConecta.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class TeamsChatConnectionTester(
    GraphConnectionTester graphConnectionTester,
    ILogger<TeamsChatConnectionTester> logger)
{
    public async Task<ChatConnectionTestResponse> TestAsync(
        bool chatEnabled,
        string authMode,
        bool azureAdConfigured,
        bool encryptionKeyConfigured,
        GraphRuntimeCredentials graphCredentials,
        CancellationToken cancellationToken)
    {
        if (!chatEnabled)
        {
            return new ChatConnectionTestResponse(
                false,
                "Integração Teams Chat desabilitada.",
                "Ative «Teams Chat — habilitado» na seção Chat e salve.",
                ChatEnabled: false,
                AuthMode: authMode);
        }

        if (!azureAdConfigured)
        {
            return new ChatConnectionTestResponse(
                false,
                "Azure AD não configurado.",
                "Configure instance, tenant ID e client ID na seção Azure AD (MSAL bootstrap).",
                ChatEnabled: true,
                AuthMode: authMode);
        }

        if (!encryptionKeyConfigured)
        {
            return new ChatConnectionTestResponse(
                false,
                "Chave de criptografia de tokens não configurada.",
                "Defina «Teams Chat — chave de criptografia de tokens» antes de vincular contas.",
                ChatEnabled: true,
                AuthMode: authMode);
        }

        if (string.IsNullOrWhiteSpace(graphCredentials.TenantId)
            || string.IsNullOrWhiteSpace(graphCredentials.ClientId)
            || string.IsNullOrWhiteSpace(graphCredentials.ClientSecret))
        {
            return new ChatConnectionTestResponse(
                false,
                "Credenciais Microsoft Graph incompletas.",
                "Configure tenant, client ID e secret na seção Microsoft Graph.",
                ChatEnabled: true,
                AuthMode: authMode);
        }

        try
        {
            var graphResult = await graphConnectionTester.TestAsync(graphCredentials, cancellationToken);
            if (!graphResult.Success)
            {
                return new ChatConnectionTestResponse(
                    false,
                    graphResult.Message,
                    graphResult.Detail,
                    ChatEnabled: true,
                    AuthMode: authMode);
            }

            return new ChatConnectionTestResponse(
                true,
                "Configuração Teams Chat validada com sucesso.",
                $"Graph OK (tenant users: {graphResult.TenantUserCount}). Auth mode: {authMode}. " +
                "Usuários devem vincular conta via MSAL para acessar conversas.",
                ChatEnabled: true,
                AuthMode: authMode);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Teams chat connection test failed.");
            return new ChatConnectionTestResponse(
                false,
                "Falha ao validar conectividade Microsoft Graph.",
                exception.Message,
                ChatEnabled: true,
                AuthMode: authMode);
        }
    }
}
