namespace LioConecta.Application.Common;

public static class BenefitPortalSettingCatalog
{
    public sealed record PortalDefinition(string BenefitKey, string Title, string DefaultUrl);

    public static IReadOnlyList<PortalDefinition> Portals { get; } =
    [
        new("plano-saude", "Plano de Saúde", "https://www.unimed.coop.br/web/guest/acesso-rapido"),
        new("plano-odonto", "Plano Odontológico", "https://www.odontoprev.com.br/colaborador"),
        new("vale-refeicao", "Vale-refeição", "https://www.meualelo.com.br"),
        new("vale-alimentacao", "Vale-alimentação", "https://www.meualelo.com.br"),
        new("vale-transporte", "Vale-transporte", "https://www.vtpass.com.br"),
        new("seguro-vida", "Seguro de Vida", "https://www.portoseguro.com.br/sinistros"),
        new("wellhub", "Wellhub (Gympass)", "https://wellhub.com/pt-br/"),
        new("home-office", "Auxílio Home Office", ""),
        new("previdencia", "Previdência Privada", "https://www.brasilprev.com.br/portal"),
        new("creche", "Auxílio Creche", ""),
        new("assistencia", "Programa de Assistência", "https://www.conexasaude.com.br"),
        new("licencas", "Licenças e Afastamentos", ""),
    ];

    public static string SettingKey(string benefitKey) =>
        $"benefits.portal.{benefitKey.Replace('-', '_')}";

    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
        Portals.Select((portal, index) => new AppSettingDefinition(
            SettingKey(portal.BenefitKey),
            "benefits",
            $"{portal.Title} — URL do portal",
            "Endereço aberto pelo botão «Abrir portal» na página de Benefícios. Deixe vazio para ocultar o botão.",
            "url",
            false,
            portal.DefaultUrl,
            index + 1));
}
