namespace LioConecta.Application.Common;

public static class RamaisSettingCatalog
{
    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
    [
        new(AppSettingKeys.RamaisAllowedRoles, "ramais", "Ramais — perfis com permissao de gestao",
            "JSON array de roles autorizadas a criar/editar/excluir ramais (ex.: HR).", "json", false, "[\"HR\"]", 1),
        new(AppSettingKeys.RamaisAllowedEmails, "ramais", "Ramais — e-mails com permissao de gestao",
            "JSON array de e-mails autorizados a gerir a lista de ramais sem depender da role.", "json", false, "[]", 2),
    ];
}