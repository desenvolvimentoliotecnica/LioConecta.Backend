using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Seed;

internal sealed record BookmarkCatalogSeedRow(
    string SeedKey,
    string Kind,
    string Title,
    string Excerpt,
    string Href,
    string Icon,
    string Source,
    int SortOrder);

internal static class BookmarkCatalogSeed
{
    public static IReadOnlyList<BookmarkCatalogSeedRow> Rows { get; } =
    [
        new("bm-estrategia-2026", "comunicado", "Atualização importante sobre nossa estratégia 2026", "Diretrizes estratégicas, bem-estar e inovação para todos os colaboradores.", "/comunicados/leitura?id=estrategia-2026", "fa-bullhorn", "Comunicados · Oficiais", 10),
        new("bm-seguranca-info", "comunicado", "Nova política de segurança da informação", "Atualização de senhas e treinamento obrigatório de segurança.", "/comunicados/leitura?id=seguranca-informacao", "fa-bullhorn", "Comunicados · Urgentes", 20),
        new("bm-enquete-hibrido", "feed", "Enquete: modelo de trabalho híbrido", "Qual formato você prefere para a rotina da sua equipe?", "/", "fa-square-poll-vertical", "Feed · Enquete", 30),
        new("bm-promocao-maria", "feed", "Parabenização: Maria Silva", "Promoção a Gerente de Projetos — Comercial.", "/", "fa-champagne-glasses", "Feed · Celebração", 40),
        new("bm-politica-viagens", "documento", "Política de viagens e despesas", "Regras para solicitação de reembolso, adiantamento e prestação de contas.", "/documentos/biblioteca", "fa-file-lines", "Documentos · Biblioteca", 50),
        new("bm-form-ferias", "documento", "Formulário de solicitação de férias", "Modelo oficial para registro de férias e ausências programadas.", "/documentos/formularios", "fa-file-lines", "Documentos · Formulários", 60),
        new("bm-reembolso-rascunho", "servico", "Reembolso de despesas — rascunho", "Solicitação em andamento, viagem São Paulo (jun/2026).", "/servicos/reembolso-despesas", "fa-receipt", "Serviços · Financeiro", 70),
        new("bm-reserva-sala", "servico", "Reserva Sala Orion — 08 jul", "Reunião de alinhamento trimestral, 14h às 16h.", "/servicos/reservas-salas", "fa-door-open", "Serviços · Facilities", 80),
        new("bm-contracheque-visualizar", "servico", "Visualizar Contracheque", "Acesse o holerite da última competência com proventos, descontos e valor líquido.", "/servicos/contracheque", "fa-file-invoice-dollar", "Serviços · RH", 90),
        new("bm-contracheque-download-pdf", "servico", "Download em PDF", "Baixe o contracheque do mês selecionado em PDF para arquivamento pessoal.", "/servicos/contracheque", "fa-file-invoice-dollar", "Serviços · RH", 100),
        new("bm-contracheque-historico", "servico", "Histórico de Holerites", "Consulte contracheques dos últimos 24 meses com busca por competência.", "/servicos/contracheque", "fa-clock-rotate-left", "Serviços · RH", 110),
        new("bm-contracheque-comparativo", "servico", "Comparativo Salarial", "Compare proventos, descontos e líquido entre dois meses.", "/servicos/contracheque", "fa-circle-question", "Serviços · RH", 120),
        new("bm-contracheque-demonstrativo", "servico", "Demonstrativo Detalhado", "Visualize rubricas, bases de cálculo e descontos linha a linha.", "/servicos/contracheque", "fa-file-invoice-dollar", "Serviços · RH", 130),
        new("bm-contracheque-informe-rendimentos", "servico", "Informe de Rendimentos", "Emita o informe anual para declaração de Imposto de Renda.", "/servicos/contracheque", "fa-receipt", "Serviços · RH", 140),
        new("bm-contracheque-comprovante", "servico", "Comprovante de Rendimentos", "Documento simplificado para comprovação de renda.", "/servicos/contracheque", "fa-receipt", "Serviços · RH", 150),
        new("bm-contracheque-carta-consignacao", "servico", "Carta de Consignação", "Consulte margem consignável e emita carta para empréstimos.", "/servicos/contracheque", "fa-file-lines", "Serviços · RH", 160),
        new("bm-contracheque-fgts", "servico", "FGTS e Encargos", "Resumo de depósitos de FGTS e encargos vinculados ao contrato.", "/servicos/contracheque", "fa-circle-question", "Serviços · RH", 170),
        new("bm-contracheque-descontos", "servico", "Descontos em Folha", "Detalhamento de plano de saúde, VT, consignados e outros descontos.", "/servicos/contracheque", "fa-circle-question", "Serviços · RH", 180),
        new("bm-contracheque-segunda-via", "servico", "Solicitar 2ª Via", "Peça reemissão de holerite de competências anteriores.", "/servicos/contracheque", "fa-file-lines", "Serviços · RH", 190),
        new("bm-contracheque-duvidas-rubricas", "servico", "Dúvidas sobre Rubricas", "Orientações sobre códigos, siglas e regras de cálculo na folha.", "/servicos/contracheque", "fa-circle-question", "Serviços · RH", 200),
    ];

    public static BookmarkCatalogItem ToEntity(BookmarkCatalogSeedRow row, DateTimeOffset seedTime) => new()
    {
        Id = Guid.NewGuid(),
        SeedKey = row.SeedKey,
        Kind = row.Kind,
        Title = row.Title,
        Excerpt = row.Excerpt,
        Href = row.Href,
        Icon = row.Icon,
        Source = row.Source,
        IsDefault = !row.SeedKey.StartsWith("bm-contracheque-", StringComparison.OrdinalIgnoreCase),
        SortOrder = row.SortOrder,
        IsActive = true,
        CreatedAt = seedTime,
        UpdatedAt = seedTime,
    };
}
