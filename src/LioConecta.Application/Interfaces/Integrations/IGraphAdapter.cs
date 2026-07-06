using LioConecta.Application.Interfaces.Integrations.Models;

namespace LioConecta.Application.Interfaces.Integrations;

public interface IGraphAdapter
{
    Task<IReadOnlyList<GraphDirectoryUser>> GetDirectoryUsersAsync(CancellationToken cancellationToken = default);

    Task<byte[]?> GetUserPhotoBytesAsync(Guid objectId, CancellationToken cancellationToken = default);

    Task SyncUserPhotosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraphDocument>> GetDocumentsAsync(string? category, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraphCalendarEvent>> GetCalendarEventsAsync(
        Guid personId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GraphPlannerTask>> GetPlannerTasksAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<string?> GetUserPresenceAsync(Guid personId, CancellationToken cancellationToken = default);
}
