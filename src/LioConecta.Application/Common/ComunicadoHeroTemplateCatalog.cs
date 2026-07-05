namespace LioConecta.Application.Common;

public sealed record ComunicadoHeroTemplateDefinition(
    string Id,
    string Label,
    string Url,
    string? Category);

public static class ComunicadoHeroTemplateCatalog
{
    public static IReadOnlyList<ComunicadoHeroTemplateDefinition> All { get; } =
    [
        new("announcement", "Comunicado geral", "/bg-announcement.png", "Geral"),
        new("security", "Segurança", "/bg-comunicado-security.png", "Compliance"),
        new("benefits", "Benefícios", "/bg-benefits.png", "RH"),
        new("news", "Notícias", "/bg-news.png", "Institucional"),
        new("news-innovation", "Inovação", "/bg-news-innovation.png", "Institucional"),
        new("marketing-event", "Evento", "/bg-marketing-event.png", "Marketing"),
        new("celebration", "Celebração", "/bg-celebration.png", "Cultura"),
        new("celebration-promotion", "Promoção", "/bg-celebration-promotion.png", "RH"),
        new("social-coffee", "Confraternização", "/bg-social-coffee.png", "Cultura"),
        new("poll", "Enquete", "/bg-poll.png", "Engajamento"),
        new("poll-remote", "Trabalho remoto", "/bg-poll-remote.png", "Engajamento"),
    ];
}
