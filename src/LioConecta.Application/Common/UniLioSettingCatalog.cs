namespace LioConecta.Application.Common;



public static class UniLioSettingCatalog

{

    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>

    [

        new(

            AppSettingKeys.UniLioEnabled,

            "unilio",

            "UniLio — módulo habilitado",

            "Quando false, o módulo UniLio (universidade corporativa) fica indisponível no portal.",

            "boolean",

            false,

            "true",

            1),

        new(

            AppSettingKeys.UniLioAllowedRoles,

            "unilio",

            "UniLio — perfis com acesso",

            "JSON array de roles autorizadas a visualizar o módulo UniLio.",

            "json",

            false,

            "[\"Employee\",\"Manager\",\"HR\",\"Admin\"]",

            2),

        new(

            AppSettingKeys.UniLioAllowedEmails,

            "unilio",

            "UniLio — e-mails adicionais com acesso",

            "JSON array de e-mails autorizados além dos perfis configurados.",

            "json",

            false,

            "[]",

            3),

    ];

}

