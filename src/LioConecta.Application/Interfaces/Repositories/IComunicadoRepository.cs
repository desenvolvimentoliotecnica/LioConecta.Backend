using LioConecta.Application.Common;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Repositories;

public interface IComunicadoRepository
{
    Task<PagedResult<Comunicado>> GetPageAsync(
        ComunicadoKind? kind,
        bool archivedOnly,
        Guid? viewerDepartmentId,
        bool includeUnpublished,
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

    Task<Guid?> GetDepartmentIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<Comunicado?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Comunicado>> GetScheduledDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task AddAsync(Comunicado comunicado, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> HasFeedPostAsync(Guid comunicadoId, CancellationToken cancellationToken = default);
    Task AddFeedPostAsync(Comunicado comunicado, DateTimeOffset timestamp, CancellationToken cancellationToken = default);
    Task<ComunicadoMetrics> GetMetricsAsync(Comunicado comunicado, CancellationToken cancellationToken = default);
}

public sealed record ComunicadoMetrics(int EligibleReaders, int ReadCount);
