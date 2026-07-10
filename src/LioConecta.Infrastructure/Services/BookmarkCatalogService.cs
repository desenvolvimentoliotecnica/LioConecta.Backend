using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Services;

public sealed class BookmarkCatalogService(AppDbContext db) : IBookmarkCatalogService
{
    public async Task<IReadOnlyList<BookmarkCatalogItemDto>> ListCatalogAsync(CancellationToken cancellationToken = default)
    {
        var items = await db.BookmarkCatalogItems
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);

        return items
            .Select(item => new BookmarkCatalogItemDto(
                item.Id,
                item.SeedKey,
                item.Kind,
                item.Title,
                item.Excerpt,
                item.Href,
                item.Icon,
                item.Source,
                item.IsDefault,
                item.SortOrder))
            .ToList();
    }
}
