using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Seed;

internal sealed record DocumentBibliotecaSeedRow(
    string SeedKey,
    string Title,
    string Description,
    string Category,
    string MediaType,
    bool IsFeatured,
    DateTimeOffset ModifiedAt);

internal static class DocumentBibliotecaSeed
{
    public static IReadOnlyList<DocumentBibliotecaSeedRow> Rows { get; } =
    [
        new("guia-cultura", "Guia de Cultura Organizacional Lio", "E-book interativo com missão, visão, valores, comportamentos esperados e histórias que definem a identidade da companhia.", "conhecimento", "ebook", true, Date(2026, 3)),
        new("historia-30-anos", "História da Lio — 30 Anos de Trajetória", "Linha do tempo ilustrada com marcos, fundadores, expansão e evolução dos negócios ao longo de três décadas.", "historia", "publicacao", false, Date(2026, 1)),
        new("identidade-visual", "Manual de Identidade Visual", "Diretrizes oficiais de logo, cores, tipografia, aplicações e usos incorretos da marca Lio Tecnica.", "marca", "ebook", false, Date(2026, 2)),
        new("brandbook", "Brandbook Completo", "Documento ampliado de posicionamento de marca, tom de voz, arquétipos e exemplos de comunicação institucional.", "marca", "ebook", false, Date(2025, 11)),
        new("case-rh-digital", "Case: Transformação Digital no RH", "Estudo de caso sobre digitalização de processos de admissão, folha e gestão de pessoas com resultados mensuráveis.", "cases", "case", false, Date(2025, 12)),
        new("case-expansao", "Case: Expansão Comercial 2025", "Relato da estratégia de crescimento regional, metas batidas, lições aprendidas e indicadores de performance.", "cases", "case", false, Date(2026, 3)),
        new("ebook-lideranca", "E-book: Liderança Colaborativa", "Material de desenvolvimento sobre feedback, delegação, conversas difíceis e construção de times de alta performance.", "treinamentos", "ebook", false, Date(2025, 10)),
        new("trilha-onboarding", "Trilha de Onboarding — Vídeos", "Playlist com boas-vindas, tour virtual, apresentação das áreas e primeiros passos para novos colaboradores.", "treinamentos", "video", false, Date(2026, 3)),
        new("artigo-inovacao", "Artigo: Inovação e Sustentabilidade", "Publicação interna sobre práticas ESG, projetos verdes e iniciativas de inovação aberta na Lio Tecnica.", "publicacoes", "artigo", false, Date(2026, 2)),
        new("revista-lioconecta", "Revista LioConnecta — Edição 12", "Edição trimestral com entrevistas, destaques de colaboradores, novidades da empresa e agenda de eventos internos.", "publicacoes", "publicacao", false, Date(2026, 3)),
        new("fotos-institucionais", "Repositório de Fotos Institucionais", "Banco de imagens oficiais de eventos, sede, equipes e campanhas internas para uso autorizado em materiais.", "marca", "acervo", false, Date(2026, 1)),
        new("apresentacoes-historicas", "Acervo de Apresentações Históricas", "Coleção de decks estratégicos, resultados anuais e comunicados de liderança arquivados para consulta.", "historia", "acervo", false, Date(2025, 8)),
        new("icones-logos", "Biblioteca de Ícones e Logos", "Download centralizado de logos, ícones, selos e variações aprovadas para apresentações e documentos internos.", "marca", "acervo", false, Date(2026, 3)),
        new("glossario", "Glossário Corporativo", "Artigo de referência com siglas, termos técnicos, nomenclaturas de produtos e vocabulário usado na organização.", "conhecimento", "artigo", false, Date(2025, 9)),
    ];

    public static DocumentMetadata ToEntity(DocumentBibliotecaSeedRow row, DateTimeOffset seedTime) => new()
    {
        Id = Guid.NewGuid(),
        Title = row.Title,
        Description = row.Description,
        Category = row.Category,
        MediaType = row.MediaType,
        IsFeatured = row.IsFeatured,
        SeedKey = row.SeedKey,
        SharePointUrl = $"/documents/biblioteca/{row.SeedKey}.pdf",
        SharePointItemId = $"biblioteca-{row.SeedKey}",
        ModifiedAt = row.ModifiedAt,
        CreatedAt = seedTime,
        UpdatedAt = seedTime,
    };

    private static DateTimeOffset Date(int year, int month)
        => new(year, month, 1, 0, 0, 0, TimeSpan.Zero);
}
