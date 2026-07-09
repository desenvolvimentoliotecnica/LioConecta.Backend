namespace LioConecta.Application.Common;

public static class BenefitsSettingCatalog
{
    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
    [
        new(AppSettingKeys.BenefitsAllowedRoles, "benefits", "Benefícios — perfis com permissão de gestão",
            "JSON array de roles autorizadas a gerir catálogo e atribuições (ex.: HR).", "json", false, "[\"HR\"]", 1),
        new(AppSettingKeys.BenefitsAllowedEmails, "benefits", "Benefícios — e-mails com permissão de gestão",
            "JSON array de e-mails autorizados a gerir benefícios sem depender da role.", "json", false, "[]", 2),
    ];
}
