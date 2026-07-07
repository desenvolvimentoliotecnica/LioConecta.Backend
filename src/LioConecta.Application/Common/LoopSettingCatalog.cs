namespace LioConecta.Application.Common;

public static class LoopSettingCatalog
{
    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
    [
        new(
            AppSettingKeys.LoopEnabled,
            "loop",
            "Loop — módulo habilitado",
            "Quando false, o módulo Loop de Projetos fica indisponível no portal.",
            "boolean",
            false,
            "true",
            1),
        new(
            AppSettingKeys.LoopAllowedRoles,
            "loop",
            "Loop — perfis com acesso",
            "JSON array de roles autorizadas a visualizar o módulo Loop.",
            "json",
            false,
            "[\"Manager\",\"Admin\",\"AnalyticsViewer\"]",
            2),
        new(
            AppSettingKeys.LoopAllowedEmails,
            "loop",
            "Loop — e-mails adicionais com acesso",
            "JSON array de e-mails autorizados além dos perfis configurados.",
            "json",
            false,
            "[]",
            3),
    ];
}
