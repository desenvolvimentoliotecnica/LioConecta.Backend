using LioConecta.Application.Common;
using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}
