using LioConecta.Application.Common;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IComunicadoRepository
{
    Task<PagedResult<Comunicado>> GetPageAsync(
        ComunicadoKind? kind,
        bool archivedOnly,
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<Comunicado?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Comunicado?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid comunicadoId, Guid personId, CancellationToken cancellationToken = default);

    Task<bool> IsReadAsync(Guid comunicadoId, Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Comunicado>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<ComunicadoKind, int>> GetActiveCountsByKindAsync(
        CancellationToken cancellationToken = default);

    Task<int> GetArchivedCountAsync(CancellationToken cancellationToken = default);

    Task<int> GetUnreadUrgentCountAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Comunicado>> GetRecentActiveAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
