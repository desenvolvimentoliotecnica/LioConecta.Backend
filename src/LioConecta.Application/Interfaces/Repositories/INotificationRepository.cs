using LioConecta.Application.Common;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<PagedResult<Notification>> GetPageAsync(
        Guid personId,
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid personId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, Guid personId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetActivePersonsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetAllPersonsAsync(CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<Notification> notifications, CancellationToken cancellationToken = default);
}
