using System.DirectoryServices.Protocols;
using System.Net;
using LioConecta.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Ldap;

public sealed class LdapConnectionTester(ILogger<LdapConnectionTester> logger)
{
    public Task<LdapConnectionTestResponse> TestAsync(
        LdapRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            return Task.FromResult(Fail("Servidor LDAP não informado.", null));
        }

        if (string.IsNullOrWhiteSpace(settings.BindDn))
        {
            return Task.FromResult(Fail("Bind DN da conta de serviço não informado.", null));
        }

        if (string.IsNullOrWhiteSpace(settings.BindPassword))
        {
            return Task.FromResult(Fail("Senha da conta de serviço não informada.", null));
        }

        try
        {
            using var connection = LdapAuthService.CreateConnection(settings);
            connection.Credential = new NetworkCredential(settings.BindDn, settings.BindPassword);
            connection.AuthType = AuthType.Basic;
            connection.Bind();

            if (!string.IsNullOrWhiteSpace(settings.SearchBase))
            {
                var request = new SearchRequest(
                    settings.SearchBase,
                    "(objectClass=*)",
                    SearchScope.Base,
                    "dn");
                connection.SendRequest(request);
            }

            return Task.FromResult(new LdapConnectionTestResponse(
                true,
                "Conexão LDAP realizada com sucesso.",
                $"Bind OK em {settings.Host}:{settings.Port}."));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao testar conexão LDAP.");
            return Task.FromResult(Fail("Não foi possível conectar ao LDAP.", exception.Message));
        }
    }

    private static LdapConnectionTestResponse Fail(string message, string? detail) =>
        new(false, message, detail);
}
