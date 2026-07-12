using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class WikiService(
    IWikiRepository wikiRepository,
    ICurrentUserService currentUserService,
    IPermissionService permissionService) : IWikiService
{
    private const string WikiPortalPrefix = "/documentos/wiki";

    private static readonly IReadOnlyDictionary<string, string> CategoryLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["acesso"] = "Acesso",
            ["hardware"] = "Hardware",
            ["software"] = "Software",
        };

    public async Task<IReadOnlyList<WikiArticleListItemDto>> ListAsync(
        string? query,
        string? category,
        WikiArticleStatus? status,
        bool manage,
        CancellationToken cancellationToken = default)
    {
        var canManage = manage && await permissionService.HasPermissionAsync(
            "wiki.manage",
            cancellationToken: cancellationToken);

        var articles = await wikiRepository.ListAsync(
            query,
            category,
            status,
            includeUnpublished: canManage,
            cancellationToken);

        return articles.Select(ToListItem).ToList();
    }

    public async Task<IReadOnlyList<WikiCategoryDto>> GetCategoriesAsync(
        bool manage,
        CancellationToken cancellationToken = default)
    {
        var canManage = manage && await permissionService.HasPermissionAsync(
            "wiki.manage",
            cancellationToken: cancellationToken);

        var counts = await wikiRepository.GetCategoryCountsAsync(canManage, cancellationToken);
        return counts
            .OrderBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
            .Select(c => new WikiCategoryDto(
                c.Category,
                CategoryLabels.TryGetValue(c.Category, out var label) ? label : Capitalize(c.Category),
                c.Count))
            .ToList();
    }

    public async Task<WikiArticleDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var article = await wikiRepository.GetBySlugAsync(normalized, cancellationToken);
        if (article is null)
        {
            return null;
        }

        var canManage = await permissionService.HasPermissionAsync(
            "wiki.manage",
            cancellationToken: cancellationToken);
        if (article.Status != WikiArticleStatus.Published && !canManage)
        {
            return null;
        }

        return ToDto(article);
    }

    public async Task<WikiArticleDto> CreateAsync(
        CreateWikiArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionService.EnsurePermissionAsync("wiki.manage", cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            throw new ArgumentException("Category is required.", nameof(request));
        }

        if (request.Status == WikiArticleStatus.Archived)
        {
            throw new ArgumentException("A wiki article cannot be created archived.", nameof(request));
        }

        var authorId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var articleId = Guid.NewGuid();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.FromTitle(request.Title, articleId)
            : NormalizeSlug(request.Slug);

        if (await wikiRepository.SlugExistsAsync(slug, null, cancellationToken))
        {
            slug = $"{slug}-{articleId.ToString("N")[..6]}";
        }

        var article = new WikiArticle
        {
            Id = articleId,
            Slug = slug,
            Title = request.Title.Trim(),
            Summary = (request.Summary ?? string.Empty).Trim(),
            Category = request.Category.Trim().ToLowerInvariant(),
            BodyHtml = request.BodyHtml?.Trim() ?? string.Empty,
            Status = request.Status,
            AuthorId = authorId,
            PublishedAt = request.Status == WikiArticleStatus.Published ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await wikiRepository.AddAsync(article, cancellationToken);
        await wikiRepository.SaveChangesAsync(cancellationToken);
        return ToDto(article);
    }

    public async Task<WikiArticleDto> UpdateAsync(
        Guid id,
        UpdateWikiArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        var article = await GetManageableAsync(id, cancellationToken);

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ArgumentException("Title cannot be empty.");
            }

            article.Title = request.Title.Trim();
        }

        if (request.Summary is not null)
        {
            article.Summary = request.Summary.Trim();
        }

        if (request.Category is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                throw new ArgumentException("Category cannot be empty.");
            }

            article.Category = request.Category.Trim().ToLowerInvariant();
        }

        if (request.BodyHtml is not null)
        {
            article.BodyHtml = request.BodyHtml.Trim();
        }

        if (request.Slug is not null)
        {
            var slug = NormalizeSlug(request.Slug);
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new ArgumentException("Slug cannot be empty.");
            }

            if (await wikiRepository.SlugExistsAsync(slug, article.Id, cancellationToken))
            {
                throw new InvalidOperationException("Slug already exists.");
            }

            article.Slug = slug;
        }

        article.UpdatedAt = DateTimeOffset.UtcNow;
        await wikiRepository.SaveChangesAsync(cancellationToken);
        return ToDto(article);
    }

    public async Task<WikiArticleDto> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var article = await GetManageableAsync(id, cancellationToken);
        if (article.Status == WikiArticleStatus.Published)
        {
            return ToDto(article);
        }

        var now = DateTimeOffset.UtcNow;
        article.Status = WikiArticleStatus.Published;
        article.PublishedAt = now;
        article.ArchivedAt = null;
        article.UpdatedAt = now;
        await wikiRepository.SaveChangesAsync(cancellationToken);
        return ToDto(article);
    }

    public async Task<WikiArticleDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var article = await GetManageableAsync(id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        article.Status = WikiArticleStatus.Archived;
        article.ArchivedAt = now;
        article.UpdatedAt = now;
        await wikiRepository.SaveChangesAsync(cancellationToken);
        return ToDto(article);
    }

    public async Task<IReadOnlyList<HelpDeskKnowledgeArticleDto>> GetPublishedKnowledgeAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var articles = await wikiRepository.ListAsync(
            query,
            category: null,
            status: WikiArticleStatus.Published,
            includeUnpublished: false,
            cancellationToken);

        return articles
            .Select(a => new HelpDeskKnowledgeArticleDto(
                a.Id.ToString("N"),
                a.Title,
                a.Summary,
                a.Category,
                a.UpdatedAt,
                ArticleUrl(a.Slug)))
            .ToList();
    }

    private async Task<WikiArticle> GetManageableAsync(Guid id, CancellationToken cancellationToken)
    {
        await permissionService.EnsurePermissionAsync("wiki.manage", cancellationToken: cancellationToken);
        var article = await wikiRepository.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Wiki article {id} not found.");
        return article;
    }

    private static WikiArticleListItemDto ToListItem(WikiArticle article) =>
        new(
            article.Id,
            article.Slug,
            article.Title,
            article.Summary,
            article.Category,
            article.Status,
            article.UpdatedAt,
            article.PublishedAt,
            ArticleUrl(article.Slug));

    private static WikiArticleDto ToDto(WikiArticle article) =>
        new(
            article.Id,
            article.Slug,
            article.Title,
            article.Summary,
            article.Category,
            article.BodyHtml,
            article.Status,
            article.CreatedAt,
            article.UpdatedAt,
            article.PublishedAt,
            article.ArchivedAt,
            article.AuthorId,
            article.Author?.Name,
            ArticleUrl(article.Slug));

    private static string ArticleUrl(string slug) => $"{WikiPortalPrefix}/{slug}";

    private static string NormalizeSlug(string? slug) =>
        (slug ?? string.Empty).Trim().ToLowerInvariant();

    private static string Capitalize(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
}
