using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IWikiService
{
    Task<IReadOnlyList<WikiArticleListItemDto>> ListAsync(
        string? query,
        string? category,
        WikiArticleStatus? status,
        bool manage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WikiCategoryDto>> GetCategoriesAsync(
        bool manage,
        CancellationToken cancellationToken = default);

    Task<WikiArticleDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<WikiArticleDto> CreateAsync(CreateWikiArticleRequest request, CancellationToken cancellationToken = default);

    Task<WikiArticleDto> UpdateAsync(Guid id, UpdateWikiArticleRequest request, CancellationToken cancellationToken = default);

    Task<WikiArticleDto> PublishAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WikiArticleDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpDeskKnowledgeArticleDto>> GetPublishedKnowledgeAsync(
        string? query,
        CancellationToken cancellationToken = default);
}
