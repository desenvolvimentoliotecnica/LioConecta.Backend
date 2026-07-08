namespace LioConecta.Application.Common;

public static class CompassSettingCatalog
{
    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
    [
        new(
            AppSettingKeys.CompassEnabled,
            "compass",
            "Compass — módulo habilitado",
            "Quando false, o módulo Compass IBP fica indisponível no portal.",
            "boolean",
            false,
            "true",
            1),
        new(
            AppSettingKeys.CompassAllowedRoles,
            "compass",
            "Compass — perfis com acesso",
            "JSON array de roles autorizadas a visualizar o módulo Compass.",
            "json",
            false,
            "[\"Manager\",\"Admin\",\"AnalyticsViewer\"]",
            2),
        new(
            AppSettingKeys.CompassAllowedEmails,
            "compass",
            "Compass — e-mails adicionais com acesso",
            "JSON array de e-mails autorizados além dos perfis configurados.",
            "json",
            false,
            "[]",
            3),
    ];
}
