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
        new(
            AppSettingKeys.CompassDatalakeHost,
            "compass",
            "Compass — Datalake host",
            "Host PostgreSQL do Datalake (etl_hyperion) para cenários IBP.",
            "string",
            false,
            "",
            10),
        new(
            AppSettingKeys.CompassDatalakeUsername,
            "compass",
            "Compass — Datalake usuário",
            "Usuário de leitura do Datalake.",
            "string",
            false,
            "",
            11),
        new(
            AppSettingKeys.CompassDatalakePassword,
            "compass",
            "Compass — Datalake senha",
            "Senha do usuário de leitura do Datalake.",
            "secret",
            true,
            "",
            12),
        new(
            AppSettingKeys.CompassDatalakeDatabase,
            "compass",
            "Compass — Datalake database",
            "Nome do database PostgreSQL (padrão: datalake).",
            "string",
            false,
            "datalake",
            13),
        new(
            AppSettingKeys.CompassDatalakePort,
            "compass",
            "Compass — Datalake porta",
            "Porta PostgreSQL do Datalake (padrão: 5432).",
            "number",
            false,
            "5432",
            14),
    ];
}
