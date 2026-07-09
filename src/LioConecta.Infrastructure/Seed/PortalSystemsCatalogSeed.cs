using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Infrastructure.Seed;

internal sealed record PortalSystemSeedRow(
    string Key,
    string Name,
    string Slug,
    string Category,
    string DestinationType,
    string? Description,
    string? UrlDev,
    string? UrlHml,
    string? UrlPrd,
    string IconFaClass,
    int SortOrder);

internal static class PortalSystemsCatalogSeed
{
    public static IReadOnlyList<PortalSystemSeedRow> Rows { get; } =
    [
        new("fluig", "Fluig", "fluig", "ERP", "External", "Plataforma Fluig para processos e workflows.",
            "https://fluig-dev.liotecnica.mock", "https://fluig-hml.liotecnica.mock", "https://fluig.liotecnica.mock",
            "fa-diagram-project", 10),
        new("datasul", "Datasul", "datasul", "ERP", "External", "ERP Datasul TOTVS.",
            "https://datasul-dev.liotecnica.mock", "https://datasul-hml.liotecnica.mock", "https://datasul.liotecnica.mock",
            "fa-building", 20),
        new("portal-rm", "Portal RM", "portal-rm", "RH", "External", "Portal TOTVS RM para gestao de pessoas.",
            "https://rm-dev.liotecnica.mock", "https://rm-hml.liotecnica.mock", "https://rm.liotecnica.mock",
            "fa-id-card", 30),
        new("portal-totvs-mla", "Portal Totvs MLA", "portal-totvs-mla", "ERP", "External", "Portal MLA TOTVS.",
            "https://mla-dev.liotecnica.mock", "https://mla-hml.liotecnica.mock", "https://mla.liotecnica.mock",
            "fa-chart-line", 40),
        new("tms-vertical", "TMS Vertical", "tms-vertical", "Logistica", "External", "Sistema de transporte e logistica.",
            "https://tms-dev.liotecnica.mock", "https://tms-hml.liotecnica.mock", "https://tms.liotecnica.mock",
            "fa-truck", 50),
        new("deps", "Deps", "deps", "Financeiro", "External", "Gestao de credito e cobranca Deps.",
            "https://deps-dev.liotecnica.mock", "https://deps-hml.liotecnica.mock", "https://deps.liotecnica.mock",
            "fa-coins", 60),
        new("mercanet", "Mercanet", "mercanet", "Comercial", "External", "Sistema comercial Mercanet.",
            "https://mercanet-dev.liotecnica.mock", "https://mercanet-hml.liotecnica.mock", "https://mercanet.liotecnica.mock",
            "fa-store", 70),
        new("portal-recrutamento", "Portal Recrutamento e Selecao", "portal-recrutamento", "RH", "External",
            "Portal de vagas e candidatos.",
            "https://recrutamento-dev.liotecnica.mock", "https://recrutamento-hml.liotecnica.mock", "https://recrutamento.liotecnica.mock",
            "fa-user-plus", 80),
        new("loop", "Loop", "loop", "Interno", "Internal", "Gestao de projetos no LioConecta.",
            "/loop", "/loop", "/loop", "fa-infinity", 90),
        new("compass", "Compass", "compass", "Interno", "Internal", "Planejamento IBP Compass no LioConecta.",
            "/compass", "/compass", "/compass", "fa-compass", 100),
        new("pulse", "Pulse", "pulse", "Interno", "Internal", "Gestao agil Pulse no LioConecta.",
            "/pulse", "/pulse", "/pulse", "fa-heart-pulse", 110),
        new("unilio", "UniLio", "unilio", "Interno", "Internal", "Universidade corporativa LMS/LXP no LioConecta.",
            "/unilio", "/unilio", "/unilio", "fa-graduation-cap", 120),
    ];

    public static PortalSystem ToEntity(PortalSystemSeedRow row, DateTimeOffset seedTime) => new()
    {
        Id = Guid.NewGuid(),
        Name = row.Name,
        Slug = row.Slug,
        Description = row.Description,
        Category = row.Category,
        DestinationType = Enum.Parse<PortalSystemDestinationType>(row.DestinationType, true),
        UrlDev = row.UrlDev,
        UrlHml = row.UrlHml,
        UrlPrd = row.UrlPrd,
        IconKind = PortalSystemIconKind.FontAwesome,
        IconFaClass = row.IconFaClass,
        SortOrder = row.SortOrder,
        IsActive = true,
        ClickCount = 0,
        SeedKey = row.Key,
        CreatedAt = seedTime,
        UpdatedAt = seedTime,
    };
}
