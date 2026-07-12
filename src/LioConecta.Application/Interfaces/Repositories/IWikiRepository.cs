using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IWikiRepository
{
    Task<IReadOnlyList<WikiArticle>> ListAsync(
        string? query,
        string? category,
        WikiArticleStatus? status,
        bool includeUnpublished,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(string Category, int Count)>> GetCategoryCountsAsync(
        bool includeUnpublished,
        CancellationToken cancellationToken = default);

    Task<WikiArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WikiArticle?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<WikiArticle?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken cancellationToken = default);

    Task AddAsync(WikiArticle article, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
