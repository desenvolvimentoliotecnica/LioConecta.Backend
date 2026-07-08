namespace LioConecta.Application.Common;

public static class SystemsSettingCatalog
{
    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
    [
        new(AppSettingKeys.PortalEnvironment, "systems", "Ambiente do portal",
            "Ambiente desta instancia do portal: dev, hml ou prd. Usado para resolver URLs de sistemas no hub.",
            "string", false, "prd", 0),
        new(AppSettingKeys.SystemsAllowedRoles, "systems", "Sistemas — perfis com permissao de gestao",
            "JSON array de roles autorizadas a criar/editar/excluir sistemas no hub (ex.: TI).", "json", false, "[\"TI\"]", 1),
        new(AppSettingKeys.SystemsAllowedEmails, "systems", "Sistemas — e-mails com permissao de gestao",
            "JSON array de e-mails autorizados a gerir o catalogo de sistemas sem depender da role.", "json", false, "[]", 2),
    ];
}
