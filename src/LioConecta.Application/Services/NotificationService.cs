using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Services;

public sealed class NotificationService(
    INotificationRepository notificationRepository,
    ICurrentUserService currentUserService) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var page = await notificationRepository.GetPageAsync(personId, request, cancellationToken);
        var items = page.Items.Select(NotificationMapper.ToDto).ToList();
        return PagedResult<NotificationDto>.FromItems(items, page.NextCursor, page.HasMore);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return await notificationRepository.GetUnreadCountAsync(personId, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await notificationRepository.MarkAsReadAsync(id, personId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        await notificationRepository.MarkAllAsReadAsync(personId, cancellationToken);
    }
}
