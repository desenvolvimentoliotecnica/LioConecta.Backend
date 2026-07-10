using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IBookmarkCatalogService
{
    Task<IReadOnlyList<BookmarkCatalogItemDto>> ListCatalogAsync(CancellationToken cancellationToken = default);
}
