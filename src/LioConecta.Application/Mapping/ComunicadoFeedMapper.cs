using System.Net;
using System.Text.Json;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Mapping;

public static class ComunicadoFeedMapper
{
    public static FeedPost CreateFeedPost(Comunicado comunicado, DateTimeOffset timestamp)
    {
        var readerId = string.IsNullOrWhiteSpace(comunicado.Slug)
            ? comunicado.Id.ToString()
            : comunicado.Slug.Trim();

        var href = $"/comunicados/leitura?id={Uri.EscapeDataString(readerId)}";
        var kindLabel = KindLabel(comunicado.Kind);
        var title = WebUtility.HtmlEncode(comunicado.Title);
        var excerpt = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(comunicado.Excerpt)
                ? "Novo comunicado publicado no portal."
                : comunicado.Excerpt.Trim());

        var content = $"""
            <span class="tag">{kindLabel}</span>
            <p style="margin-top:10px;"><strong>{title}</strong></p>
            <p>{excerpt}</p>
            <a class="announcement__cta" href="{href}">Ler comunicado completo <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>
            """;

        var metadata = new Dictionary<string, object?>
        {
            ["comunicadoId"] = comunicado.Id.ToString(),
            ["slug"] = comunicado.Slug,
            ["kind"] = comunicado.Kind.ToString(),
            ["heroImageUrl"] = comunicado.HeroImageUrl,
            ["href"] = href,
            ["title"] = comunicado.Title,
            ["excerpt"] = comunicado.Excerpt,
        };

        return new FeedPost
        {
            Id = Guid.NewGuid(),
            AuthorId = comunicado.AuthorId,
            Type = PostType.Comunicado,
            Content = content,
            MetadataJson = JsonSerializer.Serialize(metadata),
            IsPinned = false,
            CreatedAt = comunicado.PublishedAt ?? timestamp,
            UpdatedAt = timestamp,
        };
    }

    private static string KindLabel(ComunicadoKind kind) => kind switch
    {
        ComunicadoKind.Departamental => "Comunicado departamental",
        ComunicadoKind.Urgente => "Comunicado urgente",
        ComunicadoKind.Arquivo => "Comunicado arquivado",
        _ => "Comunicado oficial",
    };
}
