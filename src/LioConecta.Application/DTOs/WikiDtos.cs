using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record WikiArticleListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Category,
    WikiArticleStatus Status,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    string Url);

public sealed record WikiArticleDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Category,
    string BodyHtml,
    WikiArticleStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ArchivedAt,
    Guid AuthorId,
    string? AuthorName,
    string Url);

public sealed record WikiCategoryDto(
    string Id,
    string Label,
    int Count);

public sealed record CreateWikiArticleRequest(
    string Title,
    string? Summary,
    string Category,
    string BodyHtml,
    string? Slug,
    WikiArticleStatus Status = WikiArticleStatus.Draft);

public sealed record UpdateWikiArticleRequest(
    string? Title,
    string? Summary,
    string? Category,
    string? BodyHtml,
    string? Slug);
