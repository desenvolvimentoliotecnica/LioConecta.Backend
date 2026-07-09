namespace LioConecta.Application.Common;



public static class PortalUiSettingCatalog

{

    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>

    [

        new(

            AppSettingKeys.PortalUiMaturityBadgesEnabled,

            "portal",

            "Exibir badges de maturidade na topbar",

            "Quando true, exibe badges de maturidade nos itens de navegação da topbar.",

            "boolean",

            false,

            "false",

            1),

        new(

            AppSettingKeys.PortalUiMaturityRoadmap,

            "portal",

            "Roadmap de maturidade do portal (JSON)",

            "JSON array com itens do roadmap de maturidade (id, label, path, status, notes).",

            "json",

            false,

            "[]",

            2),

    ];

}

