using LioConecta.Domain.Enums;

namespace LioConecta.Api.Configuration;

public sealed record ServiceTypeDefinition(
    string Id,
    string Label,
    ServiceCategory Category);

public static class ServiceTypesCatalog
{
    public static IReadOnlyList<ServiceTypeDefinition> All { get; } =
    [
        new("servicos-beneficios", "Benefícios", ServiceCategory.RH),
        new("servicos-contracheque", "Contracheque", ServiceCategory.RH),
        new("servicos-ponto", "Ponto eletrônico", ServiceCategory.RH),
        new("servicos-ferias", "Férias e ausências", ServiceCategory.RH),
        new("servicos-rh", "Solicitações RH", ServiceCategory.RH),
        new("servicos-vale-transporte", "Vale transporte", ServiceCategory.RH),
        new("servicos-seguro-vida", "Seguro de vida", ServiceCategory.RH),
        new("servicos-declaracoes-certidoes", "Declarações e certidões", ServiceCategory.RH),
        new("servicos-reembolso", "Reembolso de despesas", ServiceCategory.Financeiro),
        new("servicos-adiantamento", "Adiantamento de viagem", ServiceCategory.Financeiro),
        new("servicos-help-desk", "Help desk", ServiceCategory.TI),
        new("servicos-solicitar-equipamento", "Solicitar equipamento", ServiceCategory.TI),
        new("servicos-acesso-sistemas", "Acesso a sistemas", ServiceCategory.TI),
        new("servicos-vpn-acesso-remoto", "VPN / acesso remoto", ServiceCategory.TI),
        new("servicos-reservas-salas", "Reserva de salas", ServiceCategory.Facilities),
        new("servicos-reserva-veiculos", "Reserva de veículos", ServiceCategory.Facilities),
        new("servicos-cracha-visitantes", "Crachá de visitantes", ServiceCategory.Facilities),
        new("servicos-encomendas-correios", "Encomendas e correios", ServiceCategory.Facilities),
        new("servicos-limpeza", "Limpeza", ServiceCategory.Facilities),
        new("servicos-manutencao-predial", "Manutenção predial", ServiceCategory.Facilities),
        new("servicos-copiadora", "Copiadora", ServiceCategory.Facilities),
        new("servicos-estacionamento", "Estacionamento", ServiceCategory.Facilities),
        new("servicos-refeitorio", "Refeitório", ServiceCategory.Facilities),
        new("servicos-climatizacao", "Climatização", ServiceCategory.Facilities),
        new("servicos-gestao-residuos", "Gestão de resíduos", ServiceCategory.Facilities),
        new("servicos-assinatura-digital", "Assinatura digital", ServiceCategory.Juridico),
        new("servicos-canal-denuncias", "Canal de denúncias", ServiceCategory.Juridico),
        new("servicos-contratos-minutas", "Contratos e minutas", ServiceCategory.Juridico),
        new("servicos-lgpd-privacidade", "LGPD e privacidade", ServiceCategory.Juridico),
        new("servicos-codigo-conduta", "Código de conduta", ServiceCategory.Juridico),
        new("servicos-due-diligence", "Due diligence", ServiceCategory.Juridico),
        new("servicos-procuracoes", "Procurações", ServiceCategory.Juridico),
        new("servicos-consultoria-juridica", "Consultoria jurídica", ServiceCategory.Juridico),
    ];
}
