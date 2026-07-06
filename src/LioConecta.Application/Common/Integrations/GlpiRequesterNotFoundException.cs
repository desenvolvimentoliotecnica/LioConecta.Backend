namespace LioConecta.Application.Common.Integrations;

public sealed class GlpiRequesterNotFoundException(string email)
    : Exception($"Usuário GLPI não encontrado para o e-mail {email}. Cadastre o colaborador no GLPI com o mesmo e-mail corporativo do portal.");
