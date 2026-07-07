namespace LioConecta.Application.Common;

public static class FacilitiesMenuSettingCatalog
{
    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
    [
        new(
            AppSettingKeys.FacilitiesMenuAllowedRoles,
            "facilities",
            "Cardápio — perfis com permissão de edição",
            "JSON array de roles (ex.: Facilities, Admin).",
            "json",
            false,
            "[\"Facilities\",\"Admin\"]",
            1),
        new(
            AppSettingKeys.FacilitiesMenuAllowedEmails,
            "facilities",
            "Cardápio — e-mails com permissão de edição",
            "JSON array de e-mails autorizados a editar o cardápio semanal.",
            "json",
            false,
            "[]",
            2),
        new(
            AppSettingKeys.FacilitiesMenuEmailRecipients,
            "facilities",
            "Cardápio — destinatários padrão do e-mail semanal",
            "JSON array de e-mails usados no envio semanal quando não informados na requisição.",
            "json",
            false,
            "[]",
            3),
    ];
}
