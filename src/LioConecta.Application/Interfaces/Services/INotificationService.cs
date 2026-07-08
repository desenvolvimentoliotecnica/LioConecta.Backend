using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Interfaces.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    Task NotifyComunicadoCreatedAsync(Comunicado comunicado, CancellationToken cancellationToken = default);

    Task NotifyPollCreatedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default);

    Task NotifyPollClosedAsync(FeedPost post, Poll poll, CancellationToken cancellationToken = default);

    Task NotifyLeaveRequestCreatedAsync(
        IReadOnlyList<Guid> recipientPersonIds,
        Guid leaveRecordId,
        string summary,
        CancellationToken cancellationToken = default);
}
