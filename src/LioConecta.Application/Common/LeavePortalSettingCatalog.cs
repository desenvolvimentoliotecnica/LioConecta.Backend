namespace LioConecta.Application.Common;

public static class LeavePortalSettingCatalog
{
    public sealed record PortalDefinition(string ServiceKey, string Title, string DefaultUrl);

    public static IReadOnlyList<PortalDefinition> Portals { get; } =
    [
        new("solicitar-ferias", "Solicitar Férias", ""),
        new("saldo-ferias", "Consultar Saldo de Férias", ""),
        new("abono", "Abono Pecuniário", ""),
        new("lic-maternidade", "Licença Maternidade", ""),
        new("lic-paternidade", "Licença Paternidade", ""),
        new("lic-gala", "Licença Gala / Nojo", ""),
        new("atestado", "Registrar Atestado Médico", ""),
        new("afast-inss", "Afastamento INSS", "https://meu.inss.gov.br"),
        new("falta-justificada", "Falta Justificada", ""),
        new("banco-horas", "Banco de Horas", ""),
        new("historico", "Histórico de Ausências", ""),
        new("calendario-equipe", "Calendário da Equipe", "/calendario"),
    ];

    public static string SettingKey(string serviceKey) =>
        $"leave.portal.{serviceKey.Replace('-', '_')}";

    public static IEnumerable<AppSettingDefinition> ToAppSettingDefinitions() =>
        Portals.Select((portal, index) => new AppSettingDefinition(
            SettingKey(portal.ServiceKey),
            "leave",
            $"{portal.Title} — URL do portal",
            "Endereço aberto pelo botão «Abrir» ou ação externa na página de Férias e ausências. Deixe vazio para usar fluxo interno.",
            "url",
            false,
            portal.DefaultUrl,
            index + 1));
}
