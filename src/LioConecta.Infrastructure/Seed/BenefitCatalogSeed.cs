using System.Text.Json;
using LioConecta.Domain.Entities;

namespace LioConecta.Infrastructure.Seed;

internal sealed record BenefitLineSeed(string Label, decimal? Amount, string? Note = null);

internal sealed record BenefitDependentSeed(string Name, string Relation, decimal? MonthlyValue = null);

internal sealed record BenefitDetailsSeed(
    IReadOnlyList<BenefitLineSeed> Lines,
    IReadOnlyList<BenefitDependentSeed> Dependents,
    IReadOnlyList<string> Notes);

internal sealed record BenefitCatalogItem(
    string Key,
    string Title,
    string Desc,
    string Category,
    string Provider,
    string Status,
    bool Featured,
    decimal? MonthlyValue,
    string? PortalUrl,
    string HelpText,
    BenefitDetailsSeed Details);

public static class BenefitCatalogSeed
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public static IReadOnlyList<EmployeeBenefit> BuildForPerson(Guid personId, DateTimeOffset seedTime)
    {
        return Catalog.Select(item => new EmployeeBenefit
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            BenefitKey = item.Key,
            Title = item.Title,
            Desc = item.Desc,
            Category = item.Category,
            Provider = item.Provider,
            Status = item.Status,
            Featured = item.Featured,
            IsActive = true,
            PortalUrl = item.PortalUrl,
            HelpText = item.HelpText,
            MonthlyValue = item.MonthlyValue,
            DetailsJson = SerializeDetails(item.Details),
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        }).ToList();
    }

    private static string SerializeDetails(BenefitDetailsSeed details) =>
        JsonSerializer.Serialize(new
        {
            lines = details.Lines.Select(l => new { label = l.Label, amount = l.Amount, note = l.Note }),
            dependents = details.Dependents.Select(d => new
            {
                name = d.Name,
                relation = d.Relation,
                monthlyValue = d.MonthlyValue,
            }),
            notes = details.Notes,
        }, JsonOptions);

    private static readonly IReadOnlyList<BenefitCatalogItem> Catalog =
    [
        new(
            "plano-saude",
            "Plano de Saúde",
            "Cobertura médica e hospitalar com rede credenciada nacional, coparticipação e acomodação enfermaria ou apartamento conforme plano.",
            "saude",
            "Unimed",
            "obrigatorio",
            true,
            420m,
            "https://www.unimed.coop.br/web/guest/acesso-rapido",
            "Consulte carteirinha, rede credenciada e autorizações no portal Unimed. Alterações de plano ou dependentes devem ser solicitadas ao RH com até 30 dias de antecedência da vigência.",
            new BenefitDetailsSeed(
                [
                    new("Mensalidade titular (coparticipação)", 420m),
                    new("Acomodação", null, "Apartamento — enfermaria disponível na adesão"),
                    new("Coparticipação consulta", 35m, "Valor médio por consulta eletiva"),
                    new("Coparticipação exames", 15m, "Percentual sobre tabela referenciada"),
                ],
                [
                    new("Pedro Silva", "Cônjuge", 380m),
                    new("Ana Silva", "Filha", 280m),
                ],
                [
                    "Carteirinha digital disponível no app Unimed.",
                    "Carência cumprida — inclusão desde jan/2022.",
                ])),
        new(
            "plano-odonto",
            "Plano Odontológico",
            "Consultas, limpeza, tratamentos e ortodontia com rede referenciada e cobertura para dependentes elegíveis.",
            "saude",
            "Odontoprev",
            "opcional",
            false,
            89.90m,
            "https://www.odontoprev.com.br/colaborador",
            "Benefício opcional com desconto em folha. Inclusão de dependentes mediante comprovação de vínculo. Ortodontia sujeita a carência de 180 dias.",
            new BenefitDetailsSeed(
                [
                    new("Mensalidade titular", 89.90m),
                    new("Limite anual ortodontia", 2500m),
                    new("Desconto em folha", 89.90m),
                ],
                [
                    new("Ana Silva", "Filha", 59.90m),
                ],
                [
                    "Rede referenciada nacional com mais de 25 mil dentistas.",
                ])),
        new(
            "vale-refeicao",
            "Vale-refeição",
            "Crédito mensal em cartão bandeirado para refeições em restaurantes e estabelecimentos credenciados.",
            "alimentacao",
            "Alelo",
            "obrigatorio",
            false,
            880m,
            "https://www.meualelo.com.br",
            "Crédito disponível todo dia 1º útil do mês. Saldo não utilizado permanece na carteira por até 90 dias conforme política Alelo.",
            new BenefitDetailsSeed(
                [
                    new("Crédito mensal", 880m),
                    new("Dias úteis considerados", 22m, "Proporcional em admissões/desligamentos"),
                    new("Saldo atual (jun/2026)", 412.50m),
                ],
                [],
                [
                    "Aceito em restaurantes, lanchonetes e apps de delivery credenciados.",
                ])),
        new(
            "vale-alimentacao",
            "Vale-alimentação",
            "Benefício complementar para compras em supermercados e mercearias, conforme política vigente.",
            "alimentacao",
            "Alelo",
            "obrigatorio",
            false,
            650m,
            "https://www.meualelo.com.br",
            "Não cumulativo com vale-refeição no mesmo estabelecimento. Uso exclusivo em supermercados e mercearias credenciadas.",
            new BenefitDetailsSeed(
                [
                    new("Crédito mensal", 650m),
                    new("Saldo atual (jun/2026)", 198.30m),
                ],
                [],
                [])),
        new(
            "vale-transporte",
            "Vale-transporte",
            "Auxílio para deslocamento casa–trabalho via créditos em cartão ou reembolso conforme elegibilidade.",
            "mobilidade",
            "VT Corp",
            "obrigatorio",
            false,
            220m,
            "https://www.vtpass.com.br",
            "Desconto em folha limitado a 6% do salário base. Rotas cadastradas: linha 847 (ida) e Metrô Linha Azul — Estação Sé.",
            new BenefitDetailsSeed(
                [
                    new("Crédito mensal VT", 220m),
                    new("Desconto em folha (6%)", 198.50m),
                    new("Saldo cartão", 44.20m),
                ],
                [],
                [
                    "Recarga automática no dia 25 de cada mês.",
                ])),
        new(
            "seguro-vida",
            "Seguro de Vida",
            "Proteção financeira para colaborador e dependentes, com capital segurado e assistência funeral.",
            "familia",
            "Porto Seguro",
            "obrigatorio",
            false,
            0m,
            "https://www.portoseguro.com.br/sinistros",
            "Cobertura corporativa sem custo ao colaborador. Em caso de sinistro, acione o RH e a seguradora pelo portal ou telefone 24h.",
            new BenefitDetailsSeed(
                [
                    new("Capital segurado", 120000m),
                    new("Assistência funeral", 8000m),
                    new("Custo colaborador", 0m, "100% subsidiado pela empresa"),
                ],
                [
                    new("Pedro Silva", "Cônjuge", null),
                    new("Ana Silva", "Filha", null),
                ],
                [
                    "Apólice vigente — renovação automática anual.",
                ])),
        new(
            "wellhub",
            "Wellhub (Gympass)",
            "Acesso a academias, estúdios e apps de bem-estar com planos flexíveis subsidiados parcialmente pela empresa.",
            "qualidade",
            "Wellhub",
            "flexivel",
            false,
            79.90m,
            "https://wellhub.com/pt-br/",
            "Plano Silver ativo. Upgrade para Gold disponível com diferença de R$ 40,00 descontada em folha. Cancelamento com efeito no mês subsequente.",
            new BenefitDetailsSeed(
                [
                    new("Plano atual (Silver)", 79.90m, "Colaborador paga"),
                    new("Subsídio empresa", 120m),
                    new("Visitas no mês", 8m, "Check-ins registrados em jun/2026"),
                ],
                [],
                [
                    "Apps inclusos: meditation, terapia online e nutrição.",
                ])),
        new(
            "home-office",
            "Auxílio Home Office",
            "Reembolso mensal para internet e energia elétrica de colaboradores em regime remoto ou híbrido elegível.",
            "qualidade",
            "RH Lio",
            "flexivel",
            false,
            150m,
            null,
            "Reembolso fixo mensal para colaboradores em regime híbrido (3+ dias remoto). Comprovantes auditados semestralmente.",
            new BenefitDetailsSeed(
                [
                    new("Reembolso mensal", 150m),
                    new("Regime", null, "Híbrido — 3 dias presenciais / 2 remotos"),
                    new("Último crédito", 150m, "Competência jun/2026 — creditado em folha"),
                ],
                [],
                [])),
        new(
            "previdencia",
            "Previdência Privada",
            "Plano de previdência complementar com contratação opcional e matching parcial da contribuição pela empresa.",
            "familia",
            "Brasilprev",
            "opcional",
            false,
            350m,
            "https://www.brasilprev.com.br/portal",
            "Matching de 50% até R$ 175,00. Resgate e portabilidade conforme regulamento PGBL. Simulações disponíveis no portal Brasilprev.",
            new BenefitDetailsSeed(
                [
                    new("Contribuição colaborador", 350m),
                    new("Matching empresa (50%)", 175m),
                    new("Saldo acumulado", 28450m),
                    new("Rentabilidade 12m", 11.2m, "Percentual"),
                ],
                [],
                [
                    "Plano PGBL — dedução fiscal até 12% da renda bruta.",
                ])),
        new(
            "creche",
            "Auxílio Creche",
            "Subsídio para filhos até 6 anos conforme comprovantes e limites definidos em política de benefícios.",
            "familia",
            "RH Lio",
            "opcional",
            false,
            null,
            null,
            "Benefício mediante apresentação de nota fiscal ou boleto da instituição. Limite de R$ 650,00 por dependente. Não há dependentes elegíveis no momento.",
            new BenefitDetailsSeed(
                [
                    new("Limite por dependente", 650m),
                    new("Status", null, "Elegível — nenhum dependente na faixa etária"),
                ],
                [],
                [
                    "Solicite inclusão pelo botão Consultar quando houver dependente elegível.",
                ])),
        new(
            "assistencia",
            "Programa de Assistência",
            "Apoio psicológico, jurídico, financeiro e social 24h por telefone, app e sessões presenciais.",
            "qualidade",
            "Conexa Saúde",
            "obrigatorio",
            false,
            0m,
            "https://www.conexasaude.com.br",
            "Atendimento 24h pelo 0800 ou app Conexa. Até 6 sessões de psicologia por ano sem custo. Demais especialidades mediante encaminhamento.",
            new BenefitDetailsSeed(
                [
                    new("Sessões psicologia (ano)", 3m, "De 6 disponíveis"),
                    new("Consultas jurídicas (ano)", 1m),
                    new("Custo colaborador", 0m),
                ],
                [],
                [
                    "Sigilo absoluto — dados não compartilhados com a empresa.",
                ])),
        new(
            "licencas",
            "Licenças e Afastamentos",
            "Orientações sobre licença maternidade, paternidade, gala, nojo e demais afastamentos legais previstos em CLT.",
            "familia",
            "RH Lio",
            "obrigatorio",
            false,
            null,
            null,
            "Consulte prazos, documentação e impacto na folha. Solicitações formais pelo módulo Solicitações RH com antecedência mínima de 30 dias quando aplicável.",
            new BenefitDetailsSeed(
                [
                    new("Licença maternidade", null, "120 dias — extensível por acordo coletivo"),
                    new("Licença paternidade", null, "20 dias corridos"),
                    new("Licença gala", null, "3 dias consecutivos"),
                    new("Licença nojo", null, "2 dias consecutivos por ocorrência"),
                ],
                [],
                [
                    "Nenhum afastamento ativo no momento.",
                ])),
    ];
}
